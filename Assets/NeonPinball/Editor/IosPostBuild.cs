// iOS export sonrası düzeltmeler ve marka dosyaları (<proje>/Branding klasöründen):
//  - Xcode script sandbox'ı ve modül ayarı (IL2CPP derlemesi / UnityFramework hataları)
//  - Uygulama ikonu (tek 1024 + koyu + tinted), açılış logosu ve açılış storyboard'u
//  - Info.plist: şifreleme beyanı (ITSAppUsesNonExemptEncryption), sürüm/derleme numarası
#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace NeonPinball.Editor
{
    public static class IosPostBuild
    {
        const string InkR = "0.043", InkG = "0.047", InkB = "0.063";

        [PostProcessBuild(999)]
        public static void Fix(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            FixProject(path);
            string brand = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName, "Branding");
            if (Directory.Exists(brand)) InstallBranding(path, brand);
            else UnityEngine.Debug.LogWarning("[IosPostBuild] Branding klasörü yok: " + brand);
            FixPlist(path);
        }

        static void FixProject(string path)
        {
            string projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));
            proj.SetBuildProperty(proj.ProjectGuid(), "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
            proj.SetBuildProperty(proj.GetUnityMainTargetGuid(), "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
            proj.SetBuildProperty(proj.GetUnityFrameworkTargetGuid(), "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
            proj.SetBuildProperty(proj.TargetGuidByName("GameAssembly"), "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
            // Yeni Xcode: UnityFramework modül olarak derlenemiyor (umbrella header hatası)
            proj.SetBuildProperty(proj.GetUnityFrameworkTargetGuid(), "DEFINES_MODULE", "NO");
            File.WriteAllText(projPath, proj.WriteToString());
        }

        static void Copy(string from, string to) { if (File.Exists(from)) File.Copy(from, to, true); else UnityEngine.Debug.LogWarning("[IosPostBuild] eksik: " + from); }

        static void InstallBranding(string path, string brand)
        {
            string xc = Path.Combine(path, "Unity-iPhone/Images.xcassets");
            // --- Uygulama ikonu (tek boyut, iOS 14+; iOS 18 koyu ve tinted varyantları)
            string ai = Path.Combine(xc, "AppIcon.appiconset");
            if (Directory.Exists(ai)) Directory.Delete(ai, true);
            Directory.CreateDirectory(ai);
            Copy(Path.Combine(brand, "icon_1024.png"), Path.Combine(ai, "icon.png"));
            Copy(Path.Combine(brand, "icon_dark_1024.png"), Path.Combine(ai, "icon_dark.png"));
            Copy(Path.Combine(brand, "icon_tinted_1024.png"), Path.Combine(ai, "icon_tinted.png"));
            File.WriteAllText(Path.Combine(ai, "Contents.json"),
@"{
  ""images"" : [
    { ""filename"" : ""icon.png"", ""idiom"" : ""universal"", ""platform"" : ""ios"", ""size"" : ""1024x1024"" },
    { ""appearances"" : [ { ""appearance"" : ""luminosity"", ""value"" : ""dark"" } ], ""filename"" : ""icon_dark.png"", ""idiom"" : ""universal"", ""platform"" : ""ios"", ""size"" : ""1024x1024"" },
    { ""appearances"" : [ { ""appearance"" : ""luminosity"", ""value"" : ""tinted"" } ], ""filename"" : ""icon_tinted.png"", ""idiom"" : ""universal"", ""platform"" : ""ios"", ""size"" : ""1024x1024"" }
  ],
  ""info"" : { ""author"" : ""xcode"", ""version"" : 1 }
}");
            // --- Açılış logosu
            string ll = Path.Combine(xc, "LaunchLogo.imageset");
            if (Directory.Exists(ll)) Directory.Delete(ll, true);
            Directory.CreateDirectory(ll);
            Copy(Path.Combine(brand, "launch_logo_1x.png"), Path.Combine(ll, "launch_logo_1x.png"));
            Copy(Path.Combine(brand, "launch_logo_2x.png"), Path.Combine(ll, "launch_logo_2x.png"));
            Copy(Path.Combine(brand, "launch_logo_3x.png"), Path.Combine(ll, "launch_logo_3x.png"));
            File.WriteAllText(Path.Combine(ll, "Contents.json"),
@"{
  ""images"" : [
    { ""filename"" : ""launch_logo_1x.png"", ""idiom"" : ""universal"", ""scale"" : ""1x"" },
    { ""filename"" : ""launch_logo_2x.png"", ""idiom"" : ""universal"", ""scale"" : ""2x"" },
    { ""filename"" : ""launch_logo_3x.png"", ""idiom"" : ""universal"", ""scale"" : ""3x"" }
  ],
  ""info"" : { ""author"" : ""xcode"", ""version"" : 1 }
}");
            // --- Açılış storyboard'u (koyu zemin + ortada logo); Info.plist zaten bu adları gösteriyor
            File.WriteAllText(Path.Combine(path, "LaunchScreen-iPhone.storyboard"), Storyboard(280, 212));
            File.WriteAllText(Path.Combine(path, "LaunchScreen-iPad.storyboard"), Storyboard(380, 288));
        }

        static string Storyboard(int w, int h) =>
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""15702"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" colorMatched=""YES"" initialViewController=""NeonLaunch-VC"">
    <device id=""retina6_1"" orientation=""portrait"" appearance=""dark""/>
    <dependencies>
        <deployment identifier=""iOS""/>
        <plugIn identifier=""com.apple.InterfaceBuilder.IBCocoaTouchPlugin"" version=""15704""/>
        <capability name=""documents saved in the Xcode 8 format"" minToolsVersion=""8.0""/>
    </dependencies>
    <scenes>
        <scene sceneID=""NeonLaunch-Scene"">
            <objects>
                <viewController id=""NeonLaunch-VC"" sceneMemberID=""viewController"">
                    <layoutGuides>
                        <viewControllerLayoutGuide type=""top"" id=""NeonLaunch-Top""/>
                        <viewControllerLayoutGuide type=""bottom"" id=""NeonLaunch-Bottom""/>
                    </layoutGuides>
                    <view key=""view"" userInteractionEnabled=""NO"" contentMode=""scaleToFill"" id=""NeonLaunch-Root"" userLabel=""RootView"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""414"" height=""896""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <subviews>
                            <imageView userInteractionEnabled=""NO"" contentMode=""scaleAspectFit"" horizontalHuggingPriority=""251"" verticalHuggingPriority=""251"" image=""LaunchLogo"" translatesAutoresizingMaskIntoConstraints=""NO"" id=""NeonLaunch-Img"" userLabel=""Logo"">
                                <rect key=""frame"" x=""67"" y=""342"" width=""{w}"" height=""{h}""/>
                                <constraints>
                                    <constraint firstAttribute=""width"" constant=""{w}"" id=""NeonLaunch-W""/>
                                    <constraint firstAttribute=""height"" constant=""{h}"" id=""NeonLaunch-H""/>
                                </constraints>
                            </imageView>
                        </subviews>
                        <color key=""backgroundColor"" red=""{InkR}"" green=""{InkG}"" blue=""{InkB}"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                        <constraints>
                            <constraint firstItem=""NeonLaunch-Img"" firstAttribute=""centerX"" secondItem=""NeonLaunch-Root"" secondAttribute=""centerX"" id=""NeonLaunch-CX""/>
                            <constraint firstItem=""NeonLaunch-Img"" firstAttribute=""centerY"" secondItem=""NeonLaunch-Root"" secondAttribute=""centerY"" id=""NeonLaunch-CY""/>
                        </constraints>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""NeonLaunch-FR"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
            <point key=""canvasLocation"" x=""53"" y=""375""/>
        </scene>
    </scenes>
    <resources>
        <image name=""LaunchLogo"" width=""{w}"" height=""{h}""/>
    </resources>
</document>
";

        static void FixPlist(string path)
        {
            string p = Path.Combine(path, "Info.plist");
            var doc = new PlistDocument();
            doc.ReadFromString(File.ReadAllText(p));
            var root = doc.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);        // yalnızca işletim sisteminin standart şifrelemesi
            root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
            root.SetString("CFBundleVersion", string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) ? "1" : PlayerSettings.iOS.buildNumber);
            File.WriteAllText(p, doc.WriteToString());
        }
    }
}
#endif
