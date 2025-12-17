using EntityStates.ScavBackpack;
using RoR2;
using System.IO;
using System.Linq;

namespace ProperLoop
{
    internal class J
    {
        public static void load()
        {
            ProperSave.Loading.OnLoadingEnded += _ => load(Main.savePath);
            void load(string path)
            {
                string[][] lines = File.ReadAllLines(path).ToList().ConvertAll(x => x.Split(',')).ToArray();
                Main.loops = int.Parse(lines.FirstOrDefault(x => x[0] == "loops")[1]);
                Main.stage = int.Parse(lines.FirstOrDefault(x => x[0] == "stage")[1]);
                if (Main.ScavItemCountScale.Value) Opening.maxItemDropCount = Main.loops * 5 + Main.stage + 1;
            }
        }
    }
}
