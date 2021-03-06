﻿using DungeonGame.Client;
using DungeonUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DungeonGame
{
    /// <summary>
    /// 對遊戲伺服器進行傳送資料與接收資料
    /// </summary>
    public class ClientManager
    {
        #region 傳送資料
        /// <summary>
        /// 登入伺服器
        /// <para>1. 初始化TCP監聽</para>
        /// <para>2. 傳送名稱驗證請求與玩家名稱至伺服器</para>
        /// <para>3. 等待回傳結果</para>
        /// <para>4. 登入成功則傳送登入請求與玩家名稱至伺服器</para>
        /// </summary>
        /// <param name="name"></param>
        public void RequestLogin(string name)
        {
            svMsgStatus = ServerMessageStatus.None;
            isWaitingPlayerData = true;
            playerName = name;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                tcpThread = new Thread(Listen);
                tcpThread.IsBackground = true;
                tcpThread.Start();

                SendToServer(ClientMessageType.Verification, playerName);

                svMsgStatus = ServerMessageStatus.Waiting;
                while (svMsgStatus == ServerMessageStatus.Waiting) ;

                if (svMsgStatus == ServerMessageStatus.Success)
                {
                    status = OnlineStatus.Online;
                    SendToServer(ClientMessageType.Online, playerName);
                }

                svMsgStatus = ServerMessageStatus.None;
            }
            catch { }
        }

        /// <summary>
        /// 玩家移動後對伺服器傳送玩家的新位置
        /// </summary>
        public void RequestUpdatePlayerLocation()
        {
            SendToServer(ClientMessageType.Action, string.Format("{0}|{1}|{2}",
                playerName, Game.player.Location.X, Game.player.Location.Y));
        }

        /// <summary>
        /// 玩家對所有在線玩家發送訊息
        /// </summary>
        /// <param name="msg">玩家發送的訊息</param>
        public void SendMessage(string msg)
            => SendToServer(ClientMessageType.TextMessage, playerName + " : " + msg);

        /// <summary>
        /// 由伺服器回傳的資料進行介面更新，會不斷執行
        /// </summary>
        public void UpdateUI()
        {
            RequestPlayersData();

            players[playerName] = GetPlayerCharacter();
            playerUpdateStatus[playerName] = true;

            Game.tb_CharacterStatus.Text = players[playerName].Status;
            Game.hb_Player.Value = players[playerName].CurrentHealth;

            if (Game.focusEnemyName != "" && Game.focusEnemyName != null)
            {
                Game.tb_EnemyStatus.Text = players[Game.focusEnemyName].Status;
                Game.hb_Enemy.Value = players[Game.focusEnemyName].CurrentHealth;
            }
            else
            {
                Game.hb_Enemy.Value = 0;
            }
        }

        /// <summary>
        /// 向伺服器請求玩家狀態
        /// </summary>
        private void RequestPlayersData()
            => SendToServer(ClientMessageType.SyncPlayerData, playerName);

        /// <summary>
        /// 查詢玩家
        /// </summary>
        /// <returns>Character物件</returns>
        public PlayerCharacter GetPlayerCharacter()
            => players[playerName];

        /// <summary>
        /// 查詢指定名稱玩家
        /// </summary>
        /// <param name="name">玩家名稱</param>
        /// <returns>Character物件</returns>
        public PlayerCharacter GetPlayerCharacter(string name)
            => players[name];

        /// <summary>
        /// 向伺服器請求撿起物品
        /// </summary>
        /// <param name="p">可撿取的目標物件</param>
        public void RequestPickup(Pickable p)
            => SendToServer(ClientMessageType.PickItem, playerName + "," + p.ToString());

        public void RequestFire(string fromPlayer, string weaponNum, (int x, int y) startPoint, (int x, int y) endPoint)
        {
            string fireInfo = $"{fromPlayer}|{weaponNum}|{ItemData.GetBullet(weaponNum).lifetime}|{startPoint.x}|{startPoint.y}|{endPoint.x}|{endPoint.y}";
            SendToServer(ClientMessageType.FireSingle, fireInfo);
        }

        public void RequestHit(int damage)
            => SendToServer(ClientMessageType.Hit, playerName + "|" + damage.ToString());

        public void RequestClearItem()
            => SendToServer(ClientMessageType.ClearItem, playerName);

        /// <summary>
        /// 傳送登出請求與玩家名稱
        /// </summary>
        public void Logout()
        {
            try
            {
                SendToServer(ClientMessageType.Offline, playerName);
            }
            catch { }

            status = OnlineStatus.Offline;

            foreach (var c in Game.p_Viewport.Controls)
                if (!(c is Weapons.Projectile))
                    Game.DestroyFromViewport(c);

            players.Clear();
            playerUpdateStatus.Clear();

            socket.Close();

            GC.Collect();
        }

        /// <summary>
        /// 對伺服器傳送資料
        /// </summary>
        /// <param name="type">伺服器請求的種類指令碼</param>
        /// <param name="inMsg">欲傳送之資料</param>
        private void SendToServer(ClientMessageType type, string inMsg)
        {
            string msg = EnumEx.GetOrderByEnum(type).ToString() + ">" + inMsg;

            byte[] data = Encoding.Default.GetBytes(msg);
            socket.Send(data, 0, data.Length, SocketFlags.None);
        }
        #endregion

        #region 處理接收的資料
        /// <summary>
        /// 監聽伺服器資料，會不斷執行
        /// </summary>
        private void Listen()
        {
            EndPoint svEndpoint = socket.RemoteEndPoint;
            byte[] byteDatas = new byte[dataSize];
            int inLen;
            string rawData;
            string[] datas;
            int cmdOrder;
            ClientMessageType cmd;

            while (true)
            {
                try
                {
                    inLen = socket.ReceiveFrom(byteDatas, ref svEndpoint);
                }
                catch
                {
                    socket.Close();
                    tcpThread.Abort();
                    break;
                }

                rawData = Encoding.Default.GetString(byteDatas, 0, inLen);
                datas = rawData.Split('>');
                cmdOrder = Convert.ToInt32(datas[0]);
                cmd = EnumEx.GetEnumByOrder<ClientMessageType>(cmdOrder);

                switch (cmd)
                {
                    case ClientMessageType.Online:
                        Online(datas[1]);
                        break;

                    case ClientMessageType.Verification:
                        ContinueVerification(datas[1]);
                        break;

                    case ClientMessageType.Offline:
                        ForceOffline();
                        break;

                    case ClientMessageType.TextMessage:
                        ReceiveTextMessage(datas[1]);
                        break;

                    case ClientMessageType.SyncPlayerData:
                        SyncAllPlayersData(datas[1]);
                        break;

                    case ClientMessageType.SpawnItem:
                        AddFloorItems(datas[1]);
                        break;

                    case ClientMessageType.PickItem:
                        PickItem(datas[1]);
                        break;

                    case ClientMessageType.FireSingle:
                        FireSingle(datas[1]);
                        break;

                    case ClientMessageType.Hit:
                        GotHit(datas[1]);
                        break;

                    case ClientMessageType.Respawn:
                        Respawn(datas[1]);
                        break;

                    default:
                        Console.WriteLine("bad data: " + cmdOrder);
                        break;
                }
            }
        }
        /// <summary>
        /// 玩家上線接收自己的角色資料與載入已在服務器生成的物品資料
        /// </summary>
        /// <param name="onlineData">已封裝之資料</param>
        private void Online(string onlineData)
        {
            string[] dataArr = onlineData.Split(',');
            string dataPack = dataArr[0];
            string floorItems = dataArr[1];

            PlayerCharacter c = new PlayerCharacter(dataPack);
            players.Add(playerName, c);
            isWaitingPlayerData = false;

            if (floorItems != "")
            {
                string[] itemDatas = floorItems.Split('|');
                if (itemDatas.Length % 3 != 0) return;

                for (int i = 0; i < itemDatas.Length; i += 3)
                    Game.SpawnInViewport(new Pickable(itemDatas[i], (Convert.ToInt32(itemDatas[i + 1]), Convert.ToInt32(itemDatas[i + 2]))));
            }
        }

        /// <summary>
        /// 伺服器端驗證角色名稱
        /// </summary>
        private void ContinueVerification(string result)
        {
            int resultIdx = Convert.ToInt32(result);
            svMsgStatus = EnumEx.GetEnumByOrder<ServerMessageStatus>(resultIdx);
        }

        /// <summary>
        /// 伺服器離線，玩家自動下線
        /// </summary>
        private void ForceOffline()
        {
            Game.Destroy();
            Game.AddLog("Server offline.");
        }

        /// <summary>
        /// 接收文字訊息
        /// </summary>
        /// <param name="rawData">伺服器傳來的原始資料</param>
        private void ReceiveTextMessage(string textMessage) => Game.AddTextMessage(textMessage);

        /// <summary>
        /// 同步所有其他玩家狀態資料，並在有玩家登出時移除該玩家
        /// </summary>
        /// <param name="playerDatas">伺服器傳來的原始資料</param>
        private void SyncAllPlayersData(string playerDatas)
        {
            // 初始化判斷離線的參數
            foreach (string name in playerUpdateStatus.Keys.ToList())
                if (name != playerName)
                    playerUpdateStatus[name] = false;

            // 更新/新增在線玩家資料
            foreach (string dataPack in ExtractDataPack(playerDatas))
            {
                string name = dataPack.Split('|')[0];

                if (players.ContainsKey(name))
                {
                    players[name].UpdateByDataPack(dataPack);

                    playerUpdateStatus[name] = true;
                }
                else
                {
                    PlayerCharacter c = new PlayerCharacter(dataPack);

                    players.Add(c.Name, c);

                    Game.SpawnInViewport(players[c.Name]);

                    playerUpdateStatus.Add(c.Name, true);
                }
            }

            // 清除離線玩家角色
            foreach (string name in playerUpdateStatus.Keys.ToList())
                if (!playerUpdateStatus[name])
                {
                    Game.DestroyFromViewport(players[name]);

                    playerUpdateStatus.Remove(name);

                    players.Remove(name);

                    if (Game.focusEnemyName == name)
                    {
                        Game.focusEnemyName = "";
                        Game.tb_EnemyStatus.Text = "";
                    }
                }
        }

        /// <summary>
        /// 由伺服器傳來的資料做玩家資料的分割整理
        /// <para>格式: other_player_count,name|{datapack},name|{datapack}, ...</para>
        /// </summary>
        /// <param name="dataPacks">伺服器傳來的原始資料</param>
        /// <returns></returns>
        private IEnumerable<string> ExtractDataPack(string dataPacks)
        {
            string[] datas = dataPacks.Split(',');

            int otherPlayerNum = Convert.ToInt32(datas[0][0].ToString());

            if (otherPlayerNum < 1)
                yield break;

            for (int i = 0; i < otherPlayerNum; i++)
                yield return datas[i + 1];
        }

        /// <summary>
        /// 新增地面物品，玩家會在伺服器在地面新增物品時收到該訊號
        /// </summary>
        /// <param name="floorItem"></param>
        private void AddFloorItems(string floorItem)
        {
            string[] itemData = floorItem.Split('|');
            if (itemData.Length % 3 != 0) return;

            Game.SpawnInViewport(new Pickable(itemData[0], (Convert.ToInt32(itemData[1]), Convert.ToInt32(itemData[2]))));
        }

        /// <summary>
        /// 當任何玩家撿起物品時接收到該訊號，若物品編號帶有'-'，則為其他玩家撿到，會移除物品
        /// <para>正常撿起格式: "001"</para>
        /// <para>由地面移除格式: "-001"</para>
        /// </summary>
        /// <param name="itemInfos"></param>
        private void PickItem(string itemInfos)
        {
            Pickable p;
            if (itemInfos[0] == '-')
            {
                p = new Pickable(itemInfos.Substring(1));
            }
            else
            {
                p = new Pickable(itemInfos);
                Game.s_Slot.AddItem(p.ItemNum);
                players[playerName].itemNum = p.ItemNum;
            }

            Game.DestroyFromViewport(p);
            p.Dispose();
        }

        /// <summary>
        /// 所有玩家的開火訊號
        /// <para>格式: {fromPlayer}|{weaponNum}|{lifetime}|{startPoint.x}|{startPoint.y}|{endPoint.x}|{endPoint.y} </para>
        /// </summary>
        /// <param name="fireInfo">已封裝之資訊</param>
        private void FireSingle(string fireInfo)
        {
            string[] infos = fireInfo.Split('|');
            Weapon weapon = new Weapon();
            weapon.Fire(
                fromPlayer: infos[0],
                weaponNum: infos[1],
                startPoint: (Convert.ToInt32(infos[3]), Convert.ToInt32(infos[4])),
                endPoint: (Convert.ToInt32(infos[5]), Convert.ToInt32(infos[6])));
        }

        /// <summary>
        /// 受傷訊號，更新玩家血量
        /// </summary>
        /// <param name="newHealth">扣除傷害後的新血量</param>
        private void GotHit(string newHealth)
            => players[playerName].UpdateHealth(Convert.ToInt32(newHealth));

        /// <summary>
        /// 重生角色，更新血量與玩家位置
        /// <para>格式: {HP}|{X}|{Y} </para>
        /// </summary>
        /// <param name="respawnPack">已封裝之資訊</param>
        private void Respawn(string respawnPack)
        {
            string[] respawnDatas = respawnPack.Split('|');
            int hp = Convert.ToInt32(respawnDatas[0]);
            int x = Convert.ToInt32(respawnDatas[1]);
            int y = Convert.ToInt32(respawnDatas[2]);
            string itemNum = respawnDatas[3];
            players[playerName].Respawn(hp, x, y, itemNum);
        }
        #endregion

        private string playerName;
        private const string ip = "127.0.0.1";
        private const int port = 8800;
        private const int dataSize = 0x3ff;
        private ServerMessageStatus svMsgStatus = ServerMessageStatus.None;
        private OnlineStatus status = OnlineStatus.Offline;
        private Socket socket;
        private Thread tcpThread;

        // 客戶端狀態
        public bool IsOnline => status == OnlineStatus.Online;
        // 線上玩家清單，[玩家名稱 : 角色物件]
        public Dictionary<string, PlayerCharacter> players = new Dictionary<string, PlayerCharacter>();
        // 玩家更新狀態，若同步資料後該玩家沒更新過，則會移除該玩家，[玩家名稱 : 是否更新過]
        private readonly Dictionary<string, bool> playerUpdateStatus = new Dictionary<string, bool>();
        // 是否在等待伺服器回傳玩家狀態資料
        public bool isWaitingPlayerData = true;
    }
}
