using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThermoTactics.EditorTools
{
    /// <summary>
    /// Imports the roster v3 character art (Docs/NewCharacters/thermo_tactics_roster_v3) into Art/Units/&lt;Folder&gt;
    /// and Art/Bosses/&lt;Folder&gt;: 12 idle and 12 attack frames sliced from the strips, 8 hurt frames made from idle
    /// frame 0 (white flash, then a fading red tint with a 5 px knock-back, the same as the original 13 units) and the card.
    /// Units that already have art are skipped. Same output as Tools/import_roster_v3.py.
    /// Menu: Thermo-Tactics ▸ Steps ▸ 0. Import Roster Art (then Build Everything).
    /// </summary>
    public static class RosterArtImporter
    {
        private const string Source = "Docs/NewCharacters/thermo_tactics_roster_v3";
        private const string ArtRoot = "Assets/ThermoTactics/Art";
        private const int Frames = 12;

        // (roster folder, Unity art folder, is boss)
        private static readonly (string src, string folder, bool boss)[] Roster =
        {
            ("allies/A01_mangrove_guardian", "MangroveGuardian", false),
            ("allies/A02_coral_defender", "CoralDefender", false),
            ("allies/A03_rice_terrace_warden", "RiceTerraceWarden", false),
            ("allies/A04_forest_spirit", "ForestSpirit", false),
            ("allies/A05_solar_sentinel", "SolarSentinel", false),
            ("allies/A06_wind_caller", "WindCaller", false),
            ("allies/A07_hydro_guardian", "HydroGuardian", false),
            ("allies/A08_geothermal_sentinel", "GeothermalSentinel", false),
            ("allies/A09_bamboo_warden", "BambooWarden", false),
            ("allies/A10_seagrass_healer", "SeagrassHealer", false),
            ("allies/A11_tarsier_scout", "TarsierScout", false),
            ("allies/A12_sea_turtle_guardian", "SeaTurtleGuardian", false),
            ("allies/A13_eagle_striker", "EagleStriker", false),
            ("allies/A14_reef_sentinel", "ReefSentinel", false),
            ("allies/A15_rainforest_ancient", "RainforestAncient", false),
            ("enemies/E01_coal_brute", "CoalBrute", false),
            ("enemies/E02_diesel_drone", "DieselDrone", false),
            ("enemies/E03_smog_wraith", "SmogWraith", false),
            ("enemies/E04_deforestation_beast", "DeforestationBeast", false),
            ("enemies/E05_plastic_golem", "PlasticGolem", false),
            ("enemies/E06_factory_titan", "FactoryTitan", true),
            ("enemies/E07_oil_slick", "OilSlick", false),
            ("enemies/E08_mining_ravager", "MiningRavager", false),
            ("enemies/E09_illegal_logger", "IllegalLogger", false),
            ("enemies/E10_quarry_colossus", "QuarryColossus", false),
            ("enemies/E11_toxic_runoff", "ToxicRunoff", false),
            ("enemies/E12_methane_cloud", "MethaneCloud", false),
            ("enemies/E13_incinerator_fiend", "IncineratorFiend", false),
            ("enemies/E14_landfill_leviathan", "LandfillLeviathan", false),
            ("enemies/E15_typhoon_tyrant", "TyphoonTyrant", true),
        };

        // Hurt frames: x offset in px, amount of tint, tint colour. Measured from the original 13 units.
        private static readonly (int dx, double t, bool red)[] Hurt =
        {
            (0, 0.85, false), (-3, 0.45, false), (-5, 0.55, true), (-4, 0.45, true),
            (-3, 0.30, true), (-2, 0.18, true), (-1, 0.08, true), (0, 0.0, true),
        };

        [MenuItem("Thermo-Tactics/Steps/0. Import Roster Art")]
        public static void ImportMissing() => Import(false);

        /// <summary>Writes the art for every roster unit that has none yet. Returns the number of units imported.</summary>
        public static int Import(bool overwrite)
        {
            string project = Path.GetDirectoryName(Application.dataPath);
            string source = Path.Combine(project, Source);
            if (!Directory.Exists(source))
            {
                Debug.LogError($"[Roster] Art not found: {Source}");
                return 0;
            }

            int imported = 0;
            foreach (var u in Roster)
            {
                string outDir = Path.Combine(project, ArtRoot, u.boss ? "Bosses" : "Units", u.folder);
                if (!overwrite && File.Exists(Path.Combine(outDir, "hurt_07.png"))) continue;
                Directory.CreateDirectory(outDir);

                string id = u.src.Substring(u.src.IndexOf('/') + 1);   // A03_rice_terrace_warden
                string name = id.Substring(id.IndexOf('_') + 1);       // rice_terrace_warden
                Texture2D[] idle = Slice(Path.Combine(source, u.src, $"{name}_idle_12f.png"));
                Texture2D[] attack = Slice(Path.Combine(source, u.src, $"{name}_attack_12f.png"));
                for (int i = 0; i < Hurt.Length; i++)
                    Save(MakeHurt(idle[0], Hurt[i]), Path.Combine(outDir, $"hurt_{i:00}.png"));
                for (int i = 0; i < Frames; i++)
                {
                    Save(idle[i], Path.Combine(outDir, $"idle_{i:00}.png"));
                    Save(attack[i], Path.Combine(outDir, $"attack_{i:00}.png"));
                }
                File.Copy(Path.Combine(source, "cards", $"{id}_card.png"), Path.Combine(outDir, "card.png"), true);
                imported++;
                Debug.Log($"[Roster] Imported {u.folder} (33 files)");
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Roster] {imported} unit(s) imported.");
            return imported;
        }

        private static Texture2D Load(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        private static Texture2D[] Slice(string path)
        {
            Texture2D strip = Load(path);
            int w = strip.width / Frames, h = strip.height;
            var frames = new Texture2D[Frames];
            for (int i = 0; i < Frames; i++)
            {
                var f = new Texture2D(w, h, TextureFormat.RGBA32, false);
                f.SetPixels32(GetBlock(strip, i * w, w, h));
                f.Apply();
                frames[i] = f;
            }
            Object.DestroyImmediate(strip);
            return frames;
        }

        private static Color32[] GetBlock(Texture2D tex, int x0, int w, int h)
        {
            Color32[] all = tex.GetPixels32();
            var block = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    block[y * w + x] = all[y * tex.width + x0 + x];
            return block;
        }

        private static Texture2D MakeHurt(Texture2D baseFrame, (int dx, double t, bool red) k)
        {
            int w = baseFrame.width, h = baseFrame.height;
            Color32[] src = baseFrame.GetPixels32();
            var dst = new Color32[src.Length];
            double cr = k.red ? 230 : 255, cg = k.red ? 30 : 255, cb = k.red ? 30 : 255;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = x - k.dx;
                    if (sx < 0 || sx >= w) continue;
                    Color32 p = src[y * w + sx];
                    if (p.a == 0) continue;
                    dst[y * w + x] = new Color32(
                        (byte)(int)(p.r * (1 - k.t) + cr * k.t),
                        (byte)(int)(p.g * (1 - k.t) + cg * k.t),
                        (byte)(int)(p.b * (1 - k.t) + cb * k.t), p.a);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(dst);
            tex.Apply();
            return tex;
        }

        private static void Save(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
