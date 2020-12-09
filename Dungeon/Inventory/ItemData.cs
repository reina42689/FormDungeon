﻿using System.Collections.Generic;
using System.Drawing;

namespace DungeonGame
{
    /// <summary>
    /// 物品資料，由此提取Item類
    /// </summary>
    public static class ItemData
    {
        public static readonly Dictionary<string, Item> data = new Dictionary<string, Item>
        {
            { "000", new Item { Type = WeaponType.None,            Name = "None",            Desc = "無武器", Icon = EmptyImage                   } },
            { "001", new Item { Type = WeaponType.Rifle,           Name = "Rifle",           Desc = "步槍",   Icon = Properties.Resources.item001 } },
            { "002", new Item { Type = WeaponType.Shotgun,         Name = "Shotgun",         Desc = "霰彈槍", Icon = Properties.Resources.item002 } },
            { "003", new Item { Type = WeaponType.Sniper,          Name = "Sniper",          Desc = "狙擊槍", Icon = Properties.Resources.item003 } },
            { "004", new Item { Type = WeaponType.HeavyMachineGun, Name = "HeavyMachineGun", Desc = "重機槍", Icon = Properties.Resources.item004 } },
            { "005", new Item { Type = WeaponType.Laser,           Name = "Laser",           Desc = "雷射槍", Icon = Properties.Resources.item005 } },
            { "006", new Item { Type = WeaponType.Grenade,         Name = "Grenade",         Desc = "手榴彈", Icon = Properties.Resources.item006 } },
            { "007", new Item { Type = WeaponType.RPG,             Name = "RPG",             Desc = "火箭筒", Icon = Properties.Resources.item007 } }
        };

        private static Bitmap EmptyImage
        {
            get
            {
                Bitmap bg = new Bitmap(50, 50);
                using (Graphics g = Graphics.FromImage(bg))
                {
                    g.Clear(Color.Transparent);
                    Point pos = new Point(10, 10);
                    Size slotSize = new Size(30, 30);
                    Rectangle rect = new Rectangle(pos, slotSize);

                    g.FillRectangle(new SolidBrush(Color.Transparent), rect);
                }

                return bg;
            }
        }
    }
}
