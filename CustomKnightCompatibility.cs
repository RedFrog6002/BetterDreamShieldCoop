using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CustomKnight;
using Satchel;

namespace BetterDreamShieldCoop
{
    public static class CustomKnightCompatibility
    {
        internal static void AddCustomKnightHandlers()
        {
            SkinManager.Skinables.Add(ShieldCoop.NAME, new ShieldCoop());
            SkinManager.Skinables.Add(GrimmCoop.NAME, new GrimmCoop());
        }
    }
    public class ShieldCoop : Skinable_Tk2d
    {
        public static string NAME = "Shield_coop";
        public ShieldCoop() : base(NAME) { }
        public override Material GetMaterial() => Dreamshield.collection.materials[0];
    }
    public class GrimmCoop : Skinable_Tk2d
    {
        public static string NAME = "Grimm_coop";
        public GrimmCoop() : base(NAME) { }
        public override Material GetMaterial() => Grimmchild.collection.materials[0];
    }
}
