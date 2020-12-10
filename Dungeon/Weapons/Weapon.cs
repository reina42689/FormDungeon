﻿using System;

namespace DungeonGame
{
    /// <summary>
    /// 武器類，根據武器做相應射擊
    /// </summary>
    public class Weapon
    {
        public void Fire(string fromPlayer, string weaponNum, (int x, int y) startPoint, (int x, int y) endPoint)
        {
            double angle = CalcAngle(endPoint, startPoint);
            
            var bullet0 = ItemData.GetBullet(weaponNum);

            switch (ItemData.weaponData[weaponNum].Bullet.type)
            {
                case AmmunitionType.Mult:
                    var bullet1 = ItemData.GetBullet(weaponNum);
                    var bullet2 = ItemData.GetBullet(weaponNum);
                    bullet1.StartNormal(fromPlayer, startPoint, startPoint, GetRadians(angle + 30.0));
                    bullet2.StartNormal(fromPlayer, startPoint, startPoint, GetRadians(angle - 30.0));
                    goto case AmmunitionType.Single;

                case AmmunitionType.Single:
                    bullet0.StartNormal(fromPlayer, startPoint, endPoint, GetRadians(angle));
                    break;

                case AmmunitionType.Auto:
                case AmmunitionType.Dot:
                    bullet0.StartNormal(fromPlayer, startPoint, endPoint, GetRadians(angle));
                    break;

                case AmmunitionType.Blast:
                    bullet0.StartNormal(fromPlayer, startPoint, endPoint, GetRadians(angle));
                    break;

                default:
                    break;
            }
        }

        public double CalcAngle((int x, int y) a, (int x, int y) b)
        {
            double h = a.y - b.y;
            double w = a.x - b.x;
            if (w == 0)
                return a.y >= b.y ? 90 : 270;
            else
            {
                double atan = Math.Atan(h / w);
                double _angle = atan * 180.0 / Math.PI;

                return _angle + ((atan > 0) ? (a.x > b.x) ? 0 : 180 : (a.x > b.x) ? 360 : 180);
            }
        }

        public double GetRadians(double angle)
            => Math.PI / 180 * angle;
    }
}