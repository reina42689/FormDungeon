﻿namespace DungeonGame.Client
{
    /// <summary>
    /// 等待伺服器回傳值之狀態
    /// </summary>
    public enum ClientStatus
    {
        None,
        Waiting,
        Success,
        Fail
    }
}