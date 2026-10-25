using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Modding { internal class Placeholder {} }
namespace UnityEngine { internal class Placeholder {} }
namespace UnityEngine.Rendering { internal class Placeholder {} }
namespace ShaderStripMod
{
    public sealed partial class ShaderStripMod
    {
        readonly HashSet<string> preservedAssetNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedSpriteNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedTextureNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedMaterialNames=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedObjectPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedGroupObjectPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedProjectileObjectPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedRendererRuleKeys=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedRendererExactMatchKeys=new HashSet<string>(StringComparer.OrdinalIgnoreCase),preservedProjectileRendererExactMatchKeys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly List<RendererPreserveRule> preservedRendererRules=new List<RendererPreserveRule>();
        sealed class RendererPreserveRule { internal string SceneName,ObjectPath,RendererType,SpriteName,MaterialName,TextureName,RuleKey;internal bool Projectile; }
        bool preservedRendererRuleIndexDirty,preservedRendererRuleIndexHasFallbackRules; bool visualsActive=false;
        string preserveFilePath;string projectilePerformanceMode="Balanced";
        float preservedSyncIntervalSetting=.5f;
        const string ProjectilePerformanceCustom="Custom";
        const float PreservedRendererSyncInterval=.5f,HeavyProjectileRendererSyncInterval=.2f,MediumProjectileRendererSyncInterval=.1f,ProjectileRendererSyncInterval=.05f;
        const int HeavyProjectileRuleCount=60,MediumProjectileRuleCount=20;
        string lastLog;
        static int checks;
        static void Check(bool condition,string description) { if(!condition) throw new Exception(description);checks++; }
        void InitializeListFiles() { if(string.IsNullOrEmpty(preserveFilePath)) throw new InvalidOperationException("test path missing"); }
        string BuildDefaultPreserveFile() { return "# Defaults\npath=Default/Bench\n"; }
        string CleanUnityName(string value) { if(value=="!throw") throw new FormatException("Injected parse error");return (value??"").Trim(); }
        string CleanUnityPath(string value) { return CleanUnityName(value); }
        void Log(string value) { lastLog=value; }
        bool IsIgnoredRendererPreserveRule(RendererPreserveRule rule) { return false; }
        bool IsIgnoredProjectilePreservePath(string path) { return false; }
        void LoadSpriteMasks() {} void ReloadVisualRulesFoundation() {} void RestoreShaders() {} void RestorePromotedEffects() {} void RestorePreservedRendererDraws() {} void RestorePreservedRendererProxies() {} void MarkPreservedRendererCacheDirty() {} void ApplyStrip() {}
        public static void Main()
        {
            string folder=Path.Combine(Path.GetTempPath(),"ShaderStripMod-Persistence-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path=Path.Combine(folder,"настройки.txt");
                string first="фон=изображение\r\nmenu=GIF\n",second="фон=видео\nmenu=MP4\r\n";
                AtomicTextFile.Write(path,first);
                Check(File.ReadAllText(path)==first,"Create UTF-8 settings without changing Unicode or newlines");
                Check(!File.Exists(path+".bak"),"Initial creation does not invent a backup");
                AtomicTextFile.Write(path,second);
                Check(File.ReadAllText(path)==second,"Replace complete settings");
                Check(File.ReadAllText(path+".bak")==first,"Backup stores previous complete settings");
                AtomicTextFile.Write(path,first);
                Check(File.ReadAllText(path+".bak")==second,"Subsequent replacement refreshes backup");
                bool lockedFailed=false;
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
                    try { AtomicTextFile.Write(path,second); } catch(IOException) { lockedFailed=true; }
                Check(lockedFailed&&File.ReadAllText(path)==first,"Locked destination remains intact after failed replacement");
                Check(Directory.GetFiles(folder,"*.tmp.*").Length==0,"Failed replacement removes owned temporary file");
                bool encodedFailed=false;
                try { AtomicTextFile.Write(path,"\uD800"); } catch(EncoderFallbackException) { encodedFailed=true; }
                Check(encodedFailed&&File.ReadAllText(path)==first,"Invalid UTF-8 input cannot truncate destination");
                string blocker=Path.Combine(folder,"parent-file");File.WriteAllText(blocker,"existing");bool parentFailed=false;
                try { AtomicTextFile.Write(Path.Combine(blocker,"settings.txt"),second); } catch(IOException) { parentFailed=true; }
                Check(parentFailed&&File.ReadAllText(blocker)=="existing","Temporary creation failure does not modify existing data");
                var mod=new ShaderStripMod {preserveFilePath=Path.Combine(folder,"preserve.txt")};
                const string valid="path=Room/Bench\ngroupPath=Room/Decorations\nprojectilePath=Room/Fire\nsprite=bench\ntexture=atlas\nmaterial=bench-mat\nasset=legacy\nrendererPath=Room/Logo\trendererType=SpriteRenderer\tscene=Town\tsprite=logo\n";
                AtomicTextFile.Write(mod.preserveFilePath,valid);mod.LoadPreserveList();
                Check(mod.GetTotalPreserveRuleCount()==8,"Load all preserved rule kinds");
                Check(mod.preservedRendererRules.Count==1&&mod.preservedRendererRules[0].ObjectPath=="Room/Logo","Load exact renderer rule");
                var previousRule=mod.preservedRendererRules[0];
                mod.preservedRendererExactMatchKeys.Add("cached-renderer");mod.preservedProjectileRendererExactMatchKeys.Add("cached-projectile");mod.preservedRendererRuleIndexDirty=false;mod.preservedRendererRuleIndexHasFallbackRules=true;
                using(var locked=new FileStream(mod.preserveFilePath,FileMode.Open,FileAccess.Read,FileShare.None)) mod.LoadPreserveList();
                Check(mod.GetTotalPreserveRuleCount()==8&&object.ReferenceEquals(previousRule,mod.preservedRendererRules[0]),"Read failure preserves current live visibility rules");
                Check(mod.preservedRendererExactMatchKeys.Contains("cached-renderer")&&!mod.preservedRendererRuleIndexDirty&&mod.preservedRendererRuleIndexHasFallbackRules,"Read failure preserves renderer cache state");
                Check(mod.lastLog.Contains("previous entries kept"),"Failed reload reports that existing visibility survived");
                AtomicTextFile.Write(mod.preserveFilePath,"path=Different/New\nsprite=!throw\n");mod.LoadPreserveList();
                Check(mod.GetTotalPreserveRuleCount()==8&&mod.preservedObjectPaths.Contains("Room/Bench")&&!mod.preservedObjectPaths.Contains("Different/New"),"Partial parse failure rolls back all live rules");
                Check(object.ReferenceEquals(previousRule,mod.preservedRendererRules[0])&&mod.preservedRendererExactMatchKeys.Contains("cached-renderer")&&mod.preservedProjectileRendererExactMatchKeys.Contains("cached-projectile")&&!mod.preservedRendererRuleIndexDirty&&mod.preservedRendererRuleIndexHasFallbackRules,"Partial parse failure restores rule identity and exact-match caches");
                mod.SavePreserveList();string saved=File.ReadAllText(mod.preserveFilePath);
                Check(saved.Contains("path=Room/Bench")&&!saved.Contains("Different/New"),"Saving after failed reload retains previous rule set");
                Check(File.ReadAllText(mod.preserveFilePath+".bak").Contains("Different/New"),"Preserve save keeps replaced source as recovery backup");
                mod.LoadPreserveList();Check(mod.GetTotalPreserveRuleCount()==8,"Saved production preserve format round trips");
                using(var locked=new FileStream(mod.preserveFilePath,FileMode.Open,FileAccess.Read,FileShare.None)) mod.SavePreserveList();
                Check(File.ReadAllText(mod.preserveFilePath)==saved&&Directory.GetFiles(folder,"*.tmp.*").Length==0,"Failed preserve save retains file and cleans temporary");
                AtomicTextFile.Write(mod.preserveFilePath,"# Empty by user choice\n");mod.LoadPreserveList();
                Check(mod.GetTotalPreserveRuleCount()==0&&mod.preservedRendererExactMatchKeys.Count==0&&mod.preservedRendererRuleIndexDirty,"Successful empty preserve list deliberately clears live rules and index");
                File.Delete(mod.preserveFilePath);mod.LoadPreserveList();
                Check(mod.preservedObjectPaths.Contains("Default/Bench"),"Missing preserve list is created atomically with defaults");
                Console.WriteLine("PASS: "+checks+" persistence assertions (production atomic file writer and preserve parser). ");
            }
            finally { Directory.Delete(folder,true); }
        }
    }
}
