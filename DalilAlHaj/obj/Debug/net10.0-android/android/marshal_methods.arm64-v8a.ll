; ModuleID = 'marshal_methods.arm64-v8a.ll'
source_filename = "marshal_methods.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.MarshalMethodName = type {
	i64, ; uint64_t id
	ptr ; char* name
}

%struct.MarshalMethodsManagedClass = type {
	i32, ; uint32_t token
	ptr ; MonoClass klass
}

@assembly_image_cache = dso_local local_unnamed_addr global [374 x ptr] zeroinitializer, align 8

; Each entry maps hash of an assembly name to an index into the `assembly_image_cache` array
@assembly_image_cache_hashes = dso_local local_unnamed_addr constant [1122 x i64] [
	i64 u0x001e58127c546039, ; 0: lib_System.Globalization.dll.so => 44
	i64 u0x0024d0f62dee05bd, ; 1: Xamarin.KotlinX.Coroutines.Core.dll => 331
	i64 u0x004df92dbfbd34f0, ; 2: lib_NetTopologySuite.dll.so => 211
	i64 u0x0071cf2d27b7d61e, ; 3: lib_Xamarin.AndroidX.SwipeRefreshLayout.dll.so => 308
	i64 u0x01109b0e4d99e61f, ; 4: System.ComponentModel.Annotations.dll => 15
	i64 u0x012194911580305f, ; 5: lib_ExoPlayer.UI.dll.so => 238
	i64 u0x014173a69d92a95c, ; 6: BruTile.XmlSerializers.dll => 180
	i64 u0x02123411c4e01926, ; 7: lib_Xamarin.AndroidX.Navigation.Runtime.dll.so => 296
	i64 u0x022e81ea9c46e03a, ; 8: lib_CommunityToolkit.Maui.Core.dll.so => 183
	i64 u0x0284512fad379f7e, ; 9: System.Runtime.Handles => 109
	i64 u0x02abedc11addc1ed, ; 10: lib_Mono.Android.Runtime.dll.so => 176
	i64 u0x02f55bf70672f5c8, ; 11: lib_System.IO.FileSystem.DriveInfo.dll.so => 50
	i64 u0x032267b2a94db371, ; 12: lib_Xamarin.AndroidX.AppCompat.dll.so => 248
	i64 u0x03621c804933a890, ; 13: System.Buffers => 9
	i64 u0x0399610510a38a38, ; 14: lib_System.Private.DataContractSerialization.dll.so => 90
	i64 u0x043032f1d071fae0, ; 15: ru/Microsoft.Maui.Controls.resources => 359
	i64 u0x044440a55165631e, ; 16: lib-cs-Microsoft.Maui.Controls.resources.dll.so => 337
	i64 u0x046eb1581a80c6b0, ; 17: vi/Microsoft.Maui.Controls.resources => 365
	i64 u0x047408741db2431a, ; 18: Xamarin.AndroidX.DynamicAnimation => 268
	i64 u0x04c74ef0f9b1afa5, ; 19: ExoPlayer.Hls => 233
	i64 u0x0517ef04e06e9f76, ; 20: System.Net.Primitives => 74
	i64 u0x0520407261d1eefe, ; 21: fr/DalilAlHaj.resources => 1
	i64 u0x0565d18c6da3de38, ; 22: Xamarin.AndroidX.RecyclerView => 300
	i64 u0x0581db89237110e9, ; 23: lib_System.Collections.dll.so => 14
	i64 u0x05989cb940b225a9, ; 24: Microsoft.Maui.dll => 208
	i64 u0x05a0cd02a6c1cd3c, ; 25: Svg.Skia.dll => 222
	i64 u0x05a1c25e78e22d87, ; 26: lib_System.Runtime.CompilerServices.Unsafe.dll.so => 106
	i64 u0x0600544dd3961080, ; 27: HarfBuzzSharp => 188
	i64 u0x06076b5d2b581f08, ; 28: zh-HK/Microsoft.Maui.Controls.resources => 366
	i64 u0x06388ffe9f6c161a, ; 29: System.Xml.Linq.dll => 161
	i64 u0x06600c4c124cb358, ; 30: System.Configuration.dll => 21
	i64 u0x067f95c5ddab55b3, ; 31: lib_Xamarin.AndroidX.Fragment.Ktx.dll.so => 273
	i64 u0x0680a433c781bb3d, ; 32: Xamarin.AndroidX.Collection.Jvm => 255
	i64 u0x069fff96ec92a91d, ; 33: System.Xml.XPath.dll => 166
	i64 u0x070b0847e18dab68, ; 34: Xamarin.AndroidX.Emoji2.ViewsHelper.dll => 270
	i64 u0x0739448d84d3b016, ; 35: lib_Xamarin.AndroidX.VectorDrawable.dll.so => 312
	i64 u0x07469f2eecce9e85, ; 36: mscorlib.dll => 172
	i64 u0x07adf62cd01e889a, ; 37: ExoPlayer.Common => 225
	i64 u0x07c57877c7ba78ad, ; 38: ru/Microsoft.Maui.Controls.resources.dll => 359
	i64 u0x07dcdc7460a0c5e4, ; 39: System.Collections.NonGeneric => 12
	i64 u0x08122e52765333c8, ; 40: lib_Microsoft.Extensions.Logging.Debug.dll.so => 203
	i64 u0x088610fc2509f69e, ; 41: lib_Xamarin.AndroidX.VectorDrawable.Animated.dll.so => 313
	i64 u0x08a7c865576bbde7, ; 42: System.Reflection.Primitives => 100
	i64 u0x08c9d051a4a817e5, ; 43: Xamarin.AndroidX.CustomView.PoolingContainer.dll => 266
	i64 u0x08f3c9788ee2153c, ; 44: Xamarin.AndroidX.DrawerLayout => 267
	i64 u0x09138715c92dba90, ; 45: lib_System.ComponentModel.Annotations.dll.so => 15
	i64 u0x0919c28b89381a0b, ; 46: lib_Microsoft.Extensions.Options.dll.so => 204
	i64 u0x092266563089ae3e, ; 47: lib_System.Collections.NonGeneric.dll.so => 12
	i64 u0x09d144a7e214d457, ; 48: System.Security.Cryptography => 131
	i64 u0x09e2b9f743db21a8, ; 49: lib_System.Reflection.Metadata.dll.so => 99
	i64 u0x0a4ff7e2ead194a4, ; 50: lib_SkiaSharp.HarfBuzz.dll.so => 216
	i64 u0x0abb3e2b271edc45, ; 51: System.Threading.Channels.dll => 145
	i64 u0x0ac76ad34327b01e, ; 52: ExoPlayer.Core => 227
	i64 u0x0b06b1feab070143, ; 53: System.Formats.Tar => 41
	i64 u0x0b3b632c3bbee20c, ; 54: sk/Microsoft.Maui.Controls.resources => 360
	i64 u0x0b6aff547b84fbe9, ; 55: Xamarin.KotlinX.Serialization.Core.Jvm => 334
	i64 u0x0be2e1f8ce4064ed, ; 56: Xamarin.AndroidX.ViewPager => 315
	i64 u0x0c3ca6cc978e2aae, ; 57: pt-BR/Microsoft.Maui.Controls.resources => 356
	i64 u0x0c59ad9fbbd43abe, ; 58: Mono.Android => 177
	i64 u0x0c65741e86371ee3, ; 59: lib_Xamarin.Android.Glide.GifDecoder.dll.so => 242
	i64 u0x0c74af560004e816, ; 60: Microsoft.Win32.Registry.dll => 7
	i64 u0x0c7790f60165fc06, ; 61: lib_Microsoft.Maui.Essentials.dll.so => 209
	i64 u0x0c83c82812e96127, ; 62: lib_System.Net.Mail.dll.so => 70
	i64 u0x0cce4bce83380b7f, ; 63: Xamarin.AndroidX.Security.SecurityCrypto => 305
	i64 u0x0cfd116e78cbc305, ; 64: lib_ShimSkiaSharp.dll.so => 214
	i64 u0x0d13cd7cce4284e4, ; 65: System.Security.SecureString => 134
	i64 u0x0d5c95da1348bb1c, ; 66: Svg.Model => 221
	i64 u0x0d63f4f73521c24f, ; 67: lib_Xamarin.AndroidX.SavedState.SavedState.Ktx.dll.so => 304
	i64 u0x0e04e702012f8463, ; 68: Xamarin.AndroidX.Emoji2 => 269
	i64 u0x0e14e73a54dda68e, ; 69: lib_System.Net.NameResolution.dll.so => 71
	i64 u0x0ec01b05613190b9, ; 70: SkiaSharp.Views.Android.dll => 217
	i64 u0x0ec1926fcf8d6380, ; 71: DalilAlHaj.dll => 2
	i64 u0x0f37dd7a62ae99af, ; 72: lib_Xamarin.AndroidX.Collection.Ktx.dll.so => 256
	i64 u0x0f598803ed2e44b2, ; 73: ExoPlayer.DataSource => 230
	i64 u0x0f5e7abaa7cf470a, ; 74: System.Net.HttpListener => 69
	i64 u0x1001f97bbe242e64, ; 75: System.IO.UnmanagedMemoryStream => 59
	i64 u0x102a31b45304b1da, ; 76: Xamarin.AndroidX.CustomView => 265
	i64 u0x1065c4cb554c3d75, ; 77: System.IO.IsolatedStorage.dll => 54
	i64 u0x10f6cfcbcf801616, ; 78: System.IO.Compression.Brotli => 45
	i64 u0x114443cdcf2091f1, ; 79: System.Security.Cryptography.Primitives => 129
	i64 u0x11a603952763e1d4, ; 80: System.Net.Mail => 70
	i64 u0x11a70d0e1009fb11, ; 81: System.Net.WebSockets.dll => 85
	i64 u0x11f26371eee0d3c1, ; 82: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll.so => 283
	i64 u0x11fbe62d469cc1c8, ; 83: Microsoft.VisualStudio.DesignTools.TapContract.dll => 371
	i64 u0x12128b3f59302d47, ; 84: lib_System.Xml.Serialization.dll.so => 163
	i64 u0x123639456fb056da, ; 85: System.Reflection.Emit.Lightweight.dll => 96
	i64 u0x12521e9764603eaa, ; 86: lib_System.Resources.Reader.dll.so => 103
	i64 u0x125b7f94acb989db, ; 87: Xamarin.AndroidX.RecyclerView.dll => 300
	i64 u0x12d3b63863d4ab0b, ; 88: lib_System.Threading.Overlapped.dll.so => 146
	i64 u0x12d72d80cde566b1, ; 89: ExoPlayer.SmoothStreaming => 236
	i64 u0x134eab1061c395ee, ; 90: System.Transactions => 156
	i64 u0x138567fa954faa55, ; 91: Xamarin.AndroidX.Browser => 252
	i64 u0x13a01de0cbc3f06c, ; 92: lib-fr-Microsoft.Maui.Controls.resources.dll.so => 343
	i64 u0x13beedefb0e28a45, ; 93: lib_System.Xml.XmlDocument.dll.so => 167
	i64 u0x13f1e5e209e91af4, ; 94: lib_Java.Interop.dll.so => 174
	i64 u0x13f1e880c25d96d1, ; 95: he/Microsoft.Maui.Controls.resources => 344
	i64 u0x143d8ea60a6a4011, ; 96: Microsoft.Extensions.DependencyInjection.Abstractions => 197
	i64 u0x1497051b917530bd, ; 97: lib_System.Net.WebSockets.dll.so => 85
	i64 u0x14b78ce3adce0011, ; 98: Microsoft.VisualStudio.DesignTools.TapContract => 371
	i64 u0x14d612a531c79c05, ; 99: Xamarin.JSpecify.dll => 328
	i64 u0x14e68447938213b7, ; 100: Xamarin.AndroidX.Collection.Ktx.dll => 256
	i64 u0x152a448bd1e745a7, ; 101: Microsoft.Win32.Primitives => 6
	i64 u0x1557de0138c445f4, ; 102: lib_Microsoft.Win32.Registry.dll.so => 7
	i64 u0x15bdc156ed462f2f, ; 103: lib_System.IO.FileSystem.dll.so => 53
	i64 u0x15e300c2c1668655, ; 104: System.Resources.Writer.dll => 105
	i64 u0x15e440881244515c, ; 105: ExoPlayer.Core.dll => 227
	i64 u0x1688bf9c0b89fc8a, ; 106: ExoPlayer.Common.dll => 225
	i64 u0x16bf2a22df043a09, ; 107: System.IO.Pipes.dll => 58
	i64 u0x16c7348bc77d12df, ; 108: lib-fr-DalilAlHaj.resources.dll.so => 1
	i64 u0x16ea2b318ad2d830, ; 109: System.Security.Cryptography.Algorithms => 124
	i64 u0x16eeae54c7ebcc08, ; 110: System.Reflection.dll => 102
	i64 u0x17125c9a85b4929f, ; 111: lib_netstandard.dll.so => 173
	i64 u0x1716866f7416792e, ; 112: lib_System.Security.AccessControl.dll.so => 122
	i64 u0x174f71c46216e44a, ; 113: Xamarin.KotlinX.Coroutines.Core => 331
	i64 u0x1752c12f1e1fc00c, ; 114: System.Core => 23
	i64 u0x17b56e25558a5d36, ; 115: lib-hu-Microsoft.Maui.Controls.resources.dll.so => 347
	i64 u0x17f9358913beb16a, ; 116: System.Text.Encodings.Web => 141
	i64 u0x1809fb23f29ba44a, ; 117: lib_System.Reflection.TypeExtensions.dll.so => 101
	i64 u0x18402a709e357f3b, ; 118: lib_Xamarin.KotlinX.Serialization.Core.Jvm.dll.so => 334
	i64 u0x18a735278aef47d3, ; 119: NetTopologySuite.IO.GeoJSON4STJ.dll => 213
	i64 u0x18a9befae51bb361, ; 120: System.Net.WebClient => 81
	i64 u0x18f0ce884e87d89a, ; 121: nb/Microsoft.Maui.Controls.resources.dll => 353
	i64 u0x19777fba3c41b398, ; 122: Xamarin.AndroidX.Startup.StartupRuntime.dll => 307
	i64 u0x19a4c090f14ebb66, ; 123: System.Security.Claims => 123
	i64 u0x1a91866a319e9259, ; 124: lib_System.Collections.Concurrent.dll.so => 10
	i64 u0x1aac34d1917ba5d3, ; 125: lib_System.dll.so => 170
	i64 u0x1aad60783ffa3e5b, ; 126: lib-th-Microsoft.Maui.Controls.resources.dll.so => 362
	i64 u0x1aea8f1c3b282172, ; 127: lib_System.Net.Ping.dll.so => 73
	i64 u0x1b4b7a1d0d265fa2, ; 128: Xamarin.Android.Glide.DiskLruCache => 241
	i64 u0x1bbdb16cfa73e785, ; 129: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android => 284
	i64 u0x1bc766e07b2b4241, ; 130: Xamarin.AndroidX.ResourceInspection.Annotation.dll => 301
	i64 u0x1c753b5ff15bce1b, ; 131: Mono.Android.Runtime.dll => 176
	i64 u0x1cd47467799d8250, ; 132: System.Threading.Tasks.dll => 150
	i64 u0x1d23eafdc6dc346c, ; 133: System.Globalization.Calendars.dll => 42
	i64 u0x1da4110562816681, ; 134: Xamarin.AndroidX.Security.SecurityCrypto.dll => 305
	i64 u0x1db6820994506bf5, ; 135: System.IO.FileSystem.AccessControl.dll => 49
	i64 u0x1dbb0c2c6a999acb, ; 136: System.Diagnostics.StackTrace => 32
	i64 u0x1dcda680b17dc5bb, ; 137: lib_Xamarin.Google.Guava.FailureAccess.dll.so => 325
	i64 u0x1e31d75d2b92d80d, ; 138: ExoPlayer.dll => 224
	i64 u0x1e3d87657e9659bc, ; 139: Xamarin.AndroidX.Navigation.UI => 298
	i64 u0x1e71143913d56c10, ; 140: lib-ko-Microsoft.Maui.Controls.resources.dll.so => 351
	i64 u0x1e7c31185e2fb266, ; 141: lib_System.Threading.Tasks.Parallel.dll.so => 149
	i64 u0x1ed8fcce5e9b50a0, ; 142: Microsoft.Extensions.Options.dll => 204
	i64 u0x1f055d15d807e1b2, ; 143: System.Xml.XmlSerializer => 168
	i64 u0x1f1ed22c1085f044, ; 144: lib_System.Diagnostics.FileVersionInfo.dll.so => 30
	i64 u0x1f61df9c5b94d2c1, ; 145: lib_System.Numerics.dll.so => 88
	i64 u0x1f750bb5421397de, ; 146: lib_Xamarin.AndroidX.Tracing.Tracing.dll.so => 309
	i64 u0x20237ea48006d7a8, ; 147: lib_System.Net.WebClient.dll.so => 81
	i64 u0x209375905fcc1bad, ; 148: lib_System.IO.Compression.Brotli.dll.so => 45
	i64 u0x20a6f00ff6704fd6, ; 149: ExoPlayer.Transformer => 237
	i64 u0x20aefaf77b742b27, ; 150: ExoPlayer.Dash => 228
	i64 u0x20fab3cf2dfbc8df, ; 151: lib_System.Diagnostics.Process.dll.so => 31
	i64 u0x2110167c128cba15, ; 152: System.Globalization => 44
	i64 u0x21419508838f7547, ; 153: System.Runtime.CompilerServices.VisualC => 107
	i64 u0x2174319c0d835bc9, ; 154: System.Runtime => 121
	i64 u0x2198e5bc8b7153fa, ; 155: Xamarin.AndroidX.Annotation.Experimental.dll => 246
	i64 u0x219ea1b751a4dee4, ; 156: lib_System.IO.Compression.ZipFile.dll.so => 47
	i64 u0x21cc7e445dcd5469, ; 157: System.Reflection.Emit.ILGeneration => 95
	i64 u0x220fd4f2e7c48170, ; 158: th/Microsoft.Maui.Controls.resources => 362
	i64 u0x224538d85ed15a82, ; 159: System.IO.Pipes => 58
	i64 u0x22908438c6bed1af, ; 160: lib_System.Threading.Timer.dll.so => 153
	i64 u0x22fbc14e981e3b45, ; 161: lib_Microsoft.VisualStudio.DesignTools.MobileTapContracts.dll.so => 370
	i64 u0x237be844f1f812c7, ; 162: System.Threading.Thread.dll => 151
	i64 u0x23852b3bdc9f7096, ; 163: System.Resources.ResourceManager => 104
	i64 u0x23986dd7e5d4fc01, ; 164: System.IO.FileSystem.Primitives.dll => 51
	i64 u0x23cc21e24bf661d5, ; 165: ExoPlayer.SmoothStreaming.dll => 236
	i64 u0x2407aef2bbe8fadf, ; 166: System.Console => 22
	i64 u0x240abe014b27e7d3, ; 167: Xamarin.AndroidX.Core.dll => 261
	i64 u0x247619fe4413f8bf, ; 168: System.Runtime.Serialization.Primitives.dll => 118
	i64 u0x24de8d301281575e, ; 169: Xamarin.Android.Glide => 239
	i64 u0x252073cc3caa62c2, ; 170: fr/Microsoft.Maui.Controls.resources.dll => 343
	i64 u0x256b8d41255f01b1, ; 171: Xamarin.Google.Crypto.Tink.Android => 322
	i64 u0x2662c629b96b0b30, ; 172: lib_Xamarin.Kotlin.StdLib.dll.so => 329
	i64 u0x268c1439f13bcc29, ; 173: lib_Microsoft.Extensions.Primitives.dll.so => 205
	i64 u0x26a670e154a9c54b, ; 174: System.Reflection.Extensions.dll => 98
	i64 u0x26d077d9678fe34f, ; 175: System.IO.dll => 60
	i64 u0x273f3515de5faf0d, ; 176: id/Microsoft.Maui.Controls.resources.dll => 348
	i64 u0x2742545f9094896d, ; 177: hr/Microsoft.Maui.Controls.resources => 346
	i64 u0x274d85d83ad40513, ; 178: lib_Xamarin.AndroidX.Window.WindowCore.dll.so => 318
	i64 u0x2759af78ab94d39b, ; 179: System.Net.WebSockets => 85
	i64 u0x27b2b16f3e9de038, ; 180: Xamarin.Google.Crypto.Tink.Android.dll => 322
	i64 u0x27b410442fad6cf1, ; 181: Java.Interop.dll => 174
	i64 u0x27b97e0d52c3034a, ; 182: System.Diagnostics.Debug => 28
	i64 u0x2801845a2c71fbfb, ; 183: System.Net.Primitives.dll => 74
	i64 u0x286835e259162700, ; 184: lib_Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll.so => 299
	i64 u0x28e52865585a1ebe, ; 185: Microsoft.Extensions.Diagnostics.Abstractions.dll => 198
	i64 u0x2927d345f3daec35, ; 186: SkiaSharp.dll => 215
	i64 u0x2949f3617a02c6b2, ; 187: Xamarin.AndroidX.ExifInterface => 271
	i64 u0x29aeab763a527e52, ; 188: lib_Xamarin.AndroidX.Navigation.Common.Android.dll.so => 294
	i64 u0x29f947844fb7fc11, ; 189: Microsoft.Maui.Controls.HotReload.Forms => 369
	i64 u0x2a128783efe70ba0, ; 190: uk/Microsoft.Maui.Controls.resources.dll => 364
	i64 u0x2a3b095612184159, ; 191: lib_System.Net.NetworkInformation.dll.so => 72
	i64 u0x2a45e6c17076bfbd, ; 192: SkiaSharp.HarfBuzz.dll => 216
	i64 u0x2a6507a5ffabdf28, ; 193: System.Diagnostics.TraceSource.dll => 35
	i64 u0x2ad156c8e1354139, ; 194: fi/Microsoft.Maui.Controls.resources => 342
	i64 u0x2ad5d6b13b7a3e04, ; 195: System.ComponentModel.DataAnnotations.dll => 16
	i64 u0x2af298f63581d886, ; 196: System.Text.RegularExpressions.dll => 143
	i64 u0x2afc1c4f898552ee, ; 197: lib_System.Formats.Asn1.dll.so => 40
	i64 u0x2b148910ed40fbf9, ; 198: zh-Hant/Microsoft.Maui.Controls.resources.dll => 368
	i64 u0x2b6989d78cba9a15, ; 199: Xamarin.AndroidX.Concurrent.Futures.dll => 257
	i64 u0x2c8bd14bb93a7d82, ; 200: lib-pl-Microsoft.Maui.Controls.resources.dll.so => 355
	i64 u0x2cbd9262ca785540, ; 201: lib_System.Text.Encoding.CodePages.dll.so => 138
	i64 u0x2cc9e1fed6257257, ; 202: lib_System.Reflection.Emit.Lightweight.dll.so => 96
	i64 u0x2cd723e9fe623c7c, ; 203: lib_System.Private.Xml.Linq.dll.so => 92
	i64 u0x2d169d318a968379, ; 204: System.Threading.dll => 154
	i64 u0x2d1d1413dd10c597, ; 205: Xamarin.Google.Guava.FailureAccess => 325
	i64 u0x2d20145f27cfc1d2, ; 206: Xamarin.AndroidX.Window.WindowCore.Jvm.dll => 319
	i64 u0x2d47774b7d993f59, ; 207: sv/Microsoft.Maui.Controls.resources.dll => 361
	i64 u0x2d5ffcae1ad0aaca, ; 208: System.Data.dll => 26
	i64 u0x2db915caf23548d2, ; 209: System.Text.Json.dll => 142
	i64 u0x2dcaa0bb15a4117a, ; 210: System.IO.UnmanagedMemoryStream.dll => 59
	i64 u0x2e2ced2c3c6a1edc, ; 211: lib_System.Threading.AccessControl.dll.so => 144
	i64 u0x2e5a40c319acb800, ; 212: System.IO.FileSystem => 53
	i64 u0x2e6f1f226821322a, ; 213: el/Microsoft.Maui.Controls.resources.dll => 340
	i64 u0x2f02f94df3200fe5, ; 214: System.Diagnostics.Process => 31
	i64 u0x2f2e98e1c89b1aff, ; 215: System.Xml.ReaderWriter => 162
	i64 u0x2f5911d9ba814e4e, ; 216: System.Diagnostics.Tracing => 36
	i64 u0x2f84070a459bc31f, ; 217: lib_System.Xml.dll.so => 169
	i64 u0x309ee9eeec09a71e, ; 218: lib_Xamarin.AndroidX.Fragment.dll.so => 272
	i64 u0x30c6dda129408828, ; 219: System.IO.IsolatedStorage => 54
	i64 u0x31195fef5d8fb552, ; 220: _Microsoft.Android.Resource.Designer.dll => 373
	i64 u0x312c8ed623cbfc8d, ; 221: Xamarin.AndroidX.Window.dll => 317
	i64 u0x31496b779ed0663d, ; 222: lib_System.Reflection.DispatchProxy.dll.so => 94
	i64 u0x31eab1db7d22aaab, ; 223: lib_BruTile.XmlSerializers.dll.so => 180
	i64 u0x32243413e774362a, ; 224: Xamarin.AndroidX.CardView.dll => 253
	i64 u0x3235427f8d12dae1, ; 225: lib_System.Drawing.Primitives.dll.so => 37
	i64 u0x326256f7722d4fe5, ; 226: SkiaSharp.Views.Maui.Controls.dll => 218
	i64 u0x329753a17a517811, ; 227: fr/Microsoft.Maui.Controls.resources => 343
	i64 u0x32aa989ff07a84ff, ; 228: lib_System.Xml.ReaderWriter.dll.so => 162
	i64 u0x33829542f112d59b, ; 229: System.Collections.Immutable => 11
	i64 u0x33a31443733849fe, ; 230: lib-es-Microsoft.Maui.Controls.resources.dll.so => 341
	i64 u0x341abc357fbb4ebf, ; 231: lib_System.Net.Sockets.dll.so => 80
	i64 u0x34252b4d770aba3b, ; 232: lib_Topten.RichTextKit.dll.so => 223
	i64 u0x346a212343615ac5, ; 233: lib_System.Linq.AsyncEnumerable.dll.so => 61
	i64 u0x3496c1e2dcaf5ecc, ; 234: lib_System.IO.Pipes.AccessControl.dll.so => 57
	i64 u0x34dfd74fe2afcf37, ; 235: Microsoft.Maui => 208
	i64 u0x34e292762d9615df, ; 236: cs/Microsoft.Maui.Controls.resources.dll => 337
	i64 u0x3508234247f48404, ; 237: Microsoft.Maui.Controls => 206
	i64 u0x353590da528c9d22, ; 238: System.ComponentModel.Annotations => 15
	i64 u0x3549870798b4cd30, ; 239: lib_Xamarin.AndroidX.ViewPager2.dll.so => 316
	i64 u0x355282fc1c909694, ; 240: Microsoft.Extensions.Configuration => 194
	i64 u0x3552fc5d578f0fbf, ; 241: Xamarin.AndroidX.Arch.Core.Common => 250
	i64 u0x355c649948d55d97, ; 242: lib_System.Runtime.Intrinsics.dll.so => 113
	i64 u0x35ea9d1c6834bc8c, ; 243: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll => 287
	i64 u0x360a66b9f4afb47e, ; 244: ShimSkiaSharp => 214
	i64 u0x3628ab68db23a01a, ; 245: lib_System.Diagnostics.Tools.dll.so => 34
	i64 u0x3673b042508f5b6b, ; 246: lib_System.Runtime.Extensions.dll.so => 108
	i64 u0x36740f1a8ecdc6c4, ; 247: System.Numerics => 88
	i64 u0x36b2b50fdf589ae2, ; 248: System.Reflection.Emit.Lightweight => 96
	i64 u0x36cada77dc79928b, ; 249: System.IO.MemoryMappedFiles => 55
	i64 u0x374ef46b06791af6, ; 250: System.Reflection.Primitives.dll => 100
	i64 u0x376bf93e521a5417, ; 251: lib_Xamarin.Jetbrains.Annotations.dll.so => 327
	i64 u0x37bc29f3183003b6, ; 252: lib_System.IO.dll.so => 60
	i64 u0x380134e03b1e160a, ; 253: System.Collections.Immutable.dll => 11
	i64 u0x38049b5c59b39324, ; 254: System.Runtime.CompilerServices.Unsafe => 106
	i64 u0x385c17636bb6fe6e, ; 255: Xamarin.AndroidX.CustomView.dll => 265
	i64 u0x38869c811d74050e, ; 256: System.Net.NameResolution.dll => 71
	i64 u0x38ce437a7ba71f25, ; 257: NetTopologySuite.Features => 212
	i64 u0x393c226616977fdb, ; 258: lib_Xamarin.AndroidX.ViewPager.dll.so => 315
	i64 u0x395e37c3334cf82a, ; 259: lib-ca-Microsoft.Maui.Controls.resources.dll.so => 336
	i64 u0x39c3107c28752af1, ; 260: lib_Microsoft.Extensions.FileProviders.Abstractions.dll.so => 199
	i64 u0x3ab5859054645f72, ; 261: System.Security.Cryptography.Primitives.dll => 129
	i64 u0x3ad75090c3fac0e9, ; 262: lib_Xamarin.AndroidX.ResourceInspection.Annotation.dll.so => 301
	i64 u0x3ae44ac43a1fbdbb, ; 263: System.Runtime.Serialization => 120
	i64 u0x3b41b201a9e7ee89, ; 264: lib_DalilAlHaj.dll.so => 2
	i64 u0x3b860f9932505633, ; 265: lib_System.Text.Encoding.Extensions.dll.so => 139
	i64 u0x3be99b43dd39dd37, ; 266: Xamarin.AndroidX.SavedState.SavedState.Android => 303
	i64 u0x3c3aafb6b3a00bf6, ; 267: lib_System.Security.Cryptography.X509Certificates.dll.so => 130
	i64 u0x3c4049146b59aa90, ; 268: System.Runtime.InteropServices.JavaScript => 110
	i64 u0x3c7c495f58ac5ee9, ; 269: Xamarin.Kotlin.StdLib => 329
	i64 u0x3c7e5ed3d5db71bb, ; 270: System.Security => 135
	i64 u0x3cd9d281d402eb9b, ; 271: Xamarin.AndroidX.Browser.dll => 252
	i64 u0x3d1c50cc001a991e, ; 272: Xamarin.Google.Guava.ListenableFuture.dll => 326
	i64 u0x3d2b1913edfc08d7, ; 273: lib_System.Threading.ThreadPool.dll.so => 152
	i64 u0x3d351a9e098470a1, ; 274: lib_BruTile.dll.so => 181
	i64 u0x3d46f0b995082740, ; 275: System.Xml.Linq => 161
	i64 u0x3d8a8f400514a790, ; 276: Xamarin.AndroidX.Fragment.Ktx.dll => 273
	i64 u0x3d9c2a242b040a50, ; 277: lib_Xamarin.AndroidX.Core.dll.so => 261
	i64 u0x3dbb6b9f5ab90fa7, ; 278: lib_Xamarin.AndroidX.DynamicAnimation.dll.so => 268
	i64 u0x3e4494d9c91cf7e4, ; 279: DalilAlHaj => 2
	i64 u0x3e5441657549b213, ; 280: Xamarin.AndroidX.ResourceInspection.Annotation => 301
	i64 u0x3e57d4d195c53c2e, ; 281: System.Reflection.TypeExtensions => 101
	i64 u0x3e616ab4ed1f3f15, ; 282: lib_System.Data.dll.so => 26
	i64 u0x3f1d226e6e06db7e, ; 283: Xamarin.AndroidX.SlidingPaneLayout.dll => 306
	i64 u0x3f510adf788828dd, ; 284: System.Threading.Tasks.Extensions => 148
	i64 u0x3f6f5914291cdcf7, ; 285: Microsoft.Extensions.Hosting.Abstractions => 200
	i64 u0x407a10bb4bf95829, ; 286: lib_Xamarin.AndroidX.Navigation.Common.dll.so => 293
	i64 u0x40c6d9cbfdb8b9f7, ; 287: SkiaSharp.Views.Maui.Core.dll => 219
	i64 u0x40c98b6bd77346d4, ; 288: Microsoft.VisualBasic.dll => 5
	i64 u0x4104d92598b18888, ; 289: NetTopologySuite => 211
	i64 u0x41833cf766d27d96, ; 290: mscorlib => 172
	i64 u0x41cab042be111c34, ; 291: lib_Xamarin.AndroidX.AppCompat.AppCompatResources.dll.so => 249
	i64 u0x423a9ecc4d905a88, ; 292: lib_System.Resources.ResourceManager.dll.so => 104
	i64 u0x423bf51ae7def810, ; 293: System.Xml.XPath => 166
	i64 u0x42462ff15ddba223, ; 294: System.Resources.Reader.dll => 103
	i64 u0x4291015ff4e5ef71, ; 295: Xamarin.AndroidX.Core.ViewTree.dll => 263
	i64 u0x42a31b86e6ccc3f0, ; 296: System.Diagnostics.Contracts => 27
	i64 u0x430e95b891249788, ; 297: lib_System.Reflection.Emit.dll.so => 97
	i64 u0x43375950ec7c1b6a, ; 298: netstandard.dll => 173
	i64 u0x434c4e1d9284cdae, ; 299: Mono.Android.dll => 177
	i64 u0x43505013578652a0, ; 300: lib_Xamarin.AndroidX.Activity.Ktx.dll.so => 244
	i64 u0x437d06c381ed575a, ; 301: lib_Microsoft.VisualBasic.dll.so => 5
	i64 u0x43950f84de7cc79a, ; 302: pl/Microsoft.Maui.Controls.resources.dll => 355
	i64 u0x43c077442b230f64, ; 303: Xamarin.AndroidX.Tracing.Tracing.Android => 310
	i64 u0x43e8ca5bc927ff37, ; 304: lib_Xamarin.AndroidX.Emoji2.ViewsHelper.dll.so => 270
	i64 u0x448bd33429269b19, ; 305: Microsoft.CSharp => 3
	i64 u0x4499fa3c8e494654, ; 306: lib_System.Runtime.Serialization.Primitives.dll.so => 118
	i64 u0x4515080865a951a5, ; 307: Xamarin.Kotlin.StdLib.dll => 329
	i64 u0x4545802489b736b9, ; 308: Xamarin.AndroidX.Fragment.Ktx => 273
	i64 u0x454b4d1e66bb783c, ; 309: Xamarin.AndroidX.Lifecycle.Process => 280
	i64 u0x45aceb3561dbf4e7, ; 310: Svg.Custom => 220
	i64 u0x45c40276a42e283e, ; 311: System.Diagnostics.TraceSource => 35
	i64 u0x45d124f3a617a7d2, ; 312: lib_Svg.Custom.dll.so => 220
	i64 u0x45d443f2a29adc37, ; 313: System.AppContext.dll => 8
	i64 u0x46a4213bc97fe5ae, ; 314: lib-ru-Microsoft.Maui.Controls.resources.dll.so => 359
	i64 u0x47358bd471172e1d, ; 315: lib_System.Xml.Linq.dll.so => 161
	i64 u0x47daf4e1afbada10, ; 316: pt/Microsoft.Maui.Controls.resources => 357
	i64 u0x480b7b8a3caec4c4, ; 317: lib_ExoPlayer.Database.dll.so => 229
	i64 u0x480c0a47dd42dd81, ; 318: lib_System.IO.MemoryMappedFiles.dll.so => 55
	i64 u0x49e952f19a4e2022, ; 319: System.ObjectModel => 89
	i64 u0x49f9e6948a8131e4, ; 320: lib_Xamarin.AndroidX.VersionedParcelable.dll.so => 314
	i64 u0x4a5667b2462a664b, ; 321: lib_Xamarin.AndroidX.Navigation.UI.dll.so => 298
	i64 u0x4a7a18981dbd56bc, ; 322: System.IO.Compression.FileSystem.dll => 46
	i64 u0x4aa5c60350917c06, ; 323: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll.so => 279
	i64 u0x4b07a0ed0ab33ff4, ; 324: System.Runtime.Extensions.dll => 108
	i64 u0x4b1293102404919d, ; 325: ExoPlayer.Database => 229
	i64 u0x4b576d47ac054f3c, ; 326: System.IO.FileSystem.AccessControl => 49
	i64 u0x4b7b6532ded934b7, ; 327: System.Text.Json => 142
	i64 u0x4bf547f87e5016a8, ; 328: lib_SkiaSharp.Views.Android.dll.so => 217
	i64 u0x4c2029a97af23a8d, ; 329: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android => 289
	i64 u0x4c4ca2dbd985c7c2, ; 330: ExoPlayer.Container.dll => 226
	i64 u0x4c7755cf07ad2d5f, ; 331: System.Net.Http.Json.dll => 67
	i64 u0x4cc5f15266470798, ; 332: lib_Xamarin.AndroidX.Loader.dll.so => 290
	i64 u0x4cf6f67dc77aacd2, ; 333: System.Net.NetworkInformation.dll => 72
	i64 u0x4d3183dd245425d4, ; 334: System.Net.WebSockets.Client.dll => 84
	i64 u0x4d447523346ce7e7, ; 335: lib_Svg.Skia.dll.so => 222
	i64 u0x4d479f968a05e504, ; 336: System.Linq.Expressions.dll => 62
	i64 u0x4d55a010ffc4faff, ; 337: System.Private.Xml => 93
	i64 u0x4d5cbe77561c5b2e, ; 338: System.Web.dll => 159
	i64 u0x4d77512dbd86ee4c, ; 339: lib_Xamarin.AndroidX.Arch.Core.Common.dll.so => 250
	i64 u0x4d7793536e79c309, ; 340: System.ServiceProcess => 137
	i64 u0x4d95fccc1f67c7ca, ; 341: System.Runtime.Loader.dll => 114
	i64 u0x4db014bf0ff1c9c1, ; 342: System.Linq.AsyncEnumerable => 61
	i64 u0x4dcf44c3c9b076a2, ; 343: it/Microsoft.Maui.Controls.resources.dll => 349
	i64 u0x4dd9247f1d2c3235, ; 344: Xamarin.AndroidX.Loader.dll => 290
	i64 u0x4e1d4786fe965eeb, ; 345: ExoPlayer.DataSource.dll => 230
	i64 u0x4e2aeee78e2c4a87, ; 346: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller => 299
	i64 u0x4e32f00cb0937401, ; 347: Mono.Android.Runtime => 176
	i64 u0x4e5eea4668ac2b18, ; 348: System.Text.Encoding.CodePages => 138
	i64 u0x4ebd0c4b82c5eefc, ; 349: lib_System.Threading.Channels.dll.so => 145
	i64 u0x4ee8eaa9c9c1151a, ; 350: System.Globalization.Calendars => 42
	i64 u0x4f21ee6ef9eb527e, ; 351: ca/Microsoft.Maui.Controls.resources => 336
	i64 u0x5037f0be3c28c7a3, ; 352: lib_Microsoft.Maui.Controls.dll.so => 206
	i64 u0x50c3a29b21050d45, ; 353: System.Linq.Parallel.dll => 63
	i64 u0x5112ed116d87baf8, ; 354: CommunityToolkit.Mvvm => 185
	i64 u0x5131bbe80989093f, ; 355: Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll => 286
	i64 u0x516324a5050a7e3c, ; 356: System.Net.WebProxy => 83
	i64 u0x516d6f0b21a303de, ; 357: lib_System.Diagnostics.Contracts.dll.so => 27
	i64 u0x51bb8a2afe774e32, ; 358: System.Drawing => 38
	i64 u0x5247c5c32a4140f0, ; 359: System.Resources.Reader => 103
	i64 u0x526bb15e3c386364, ; 360: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.dll => 283
	i64 u0x526ce79eb8e90527, ; 361: lib_System.Net.Primitives.dll.so => 74
	i64 u0x52829f00b4467c38, ; 362: lib_System.Data.Common.dll.so => 24
	i64 u0x529ffe06f39ab8db, ; 363: Xamarin.AndroidX.Core => 261
	i64 u0x52ff996554dbf352, ; 364: Microsoft.Maui.Graphics => 210
	i64 u0x535f7e40e8fef8af, ; 365: lib-sk-Microsoft.Maui.Controls.resources.dll.so => 360
	i64 u0x53856f5f890e77fb, ; 366: ExoPlayer.UI.dll => 238
	i64 u0x53978aac584c666e, ; 367: lib_System.Security.Cryptography.Cng.dll.so => 125
	i64 u0x53a96d5c86c9e194, ; 368: System.Net.NetworkInformation => 72
	i64 u0x53be1038a61e8d44, ; 369: System.Runtime.InteropServices.RuntimeInformation.dll => 111
	i64 u0x53c3014b9437e684, ; 370: lib-zh-HK-Microsoft.Maui.Controls.resources.dll.so => 366
	i64 u0x53e450ebd586f842, ; 371: lib_Xamarin.AndroidX.LocalBroadcastManager.dll.so => 291
	i64 u0x5435e6f049e9bc37, ; 372: System.Security.Claims.dll => 123
	i64 u0x54795225dd1587af, ; 373: lib_System.Runtime.dll.so => 121
	i64 u0x547a34f14e5f6210, ; 374: Xamarin.AndroidX.Lifecycle.Common.dll => 275
	i64 u0x54b851bc9b470503, ; 375: Xamarin.AndroidX.Navigation.Common.Android => 294
	i64 u0x554c31f797bb0485, ; 376: lib_ExoPlayer.DataSource.dll.so => 230
	i64 u0x556e8b63b660ab8b, ; 377: Xamarin.AndroidX.Lifecycle.Common.Jvm.dll => 276
	i64 u0x5588627c9a108ec9, ; 378: System.Collections.Specialized => 13
	i64 u0x55a898e4f42e3fae, ; 379: Microsoft.VisualBasic.Core.dll => 4
	i64 u0x55fa0c610fe93bb1, ; 380: lib_System.Security.Cryptography.OpenSsl.dll.so => 128
	i64 u0x561449e1215a61e4, ; 381: lib_SkiaSharp.Views.Maui.Core.dll.so => 219
	i64 u0x563b781beb648ddb, ; 382: BruTile => 181
	i64 u0x56442b99bc64bb47, ; 383: System.Runtime.Serialization.Xml.dll => 119
	i64 u0x56a8b26e1aeae27b, ; 384: System.Threading.Tasks.Dataflow => 147
	i64 u0x56f932d61e93c07f, ; 385: System.Globalization.Extensions => 43
	i64 u0x571c5cfbec5ae8e2, ; 386: System.Private.Uri => 91
	i64 u0x57201164aeb974e3, ; 387: Xamarin.Google.Guava.FailureAccess.dll => 325
	i64 u0x576499c9f52fea31, ; 388: Xamarin.AndroidX.Annotation => 245
	i64 u0x579a06fed6eec900, ; 389: System.Private.CoreLib.dll => 179
	i64 u0x57adda3c951abb33, ; 390: Microsoft.Extensions.Hosting.Abstractions.dll => 200
	i64 u0x57c542c14049b66d, ; 391: System.Diagnostics.DiagnosticSource => 29
	i64 u0x581a8bd5cfda563e, ; 392: System.Threading.Timer => 153
	i64 u0x58601b2dda4a27b9, ; 393: lib-ja-Microsoft.Maui.Controls.resources.dll.so => 350
	i64 u0x58688d9af496b168, ; 394: Microsoft.Extensions.DependencyInjection.dll => 196
	i64 u0x588c167a79db6bfb, ; 395: lib_Xamarin.Google.ErrorProne.Annotations.dll.so => 323
	i64 u0x5906028ae5151104, ; 396: Xamarin.AndroidX.Activity.Ktx => 244
	i64 u0x595a356d23e8da9a, ; 397: lib_Microsoft.CSharp.dll.so => 3
	i64 u0x59cd492ef1d088d6, ; 398: ExoPlayer.Rtsp => 235
	i64 u0x59f9e60b9475085f, ; 399: lib_Xamarin.AndroidX.Annotation.Experimental.dll.so => 246
	i64 u0x5a745f5101a75527, ; 400: lib_System.IO.Compression.FileSystem.dll.so => 46
	i64 u0x5a89a886ae30258d, ; 401: lib_Xamarin.AndroidX.CoordinatorLayout.dll.so => 260
	i64 u0x5a8f6699f4a1caa9, ; 402: lib_System.Threading.dll.so => 154
	i64 u0x5ae9cd33b15841bf, ; 403: System.ComponentModel => 20
	i64 u0x5aeb8cd498d4823e, ; 404: lib_Xamarin.Google.Guava.dll.so => 324
	i64 u0x5b54391bdc6fcfe6, ; 405: System.Private.DataContractSerialization => 90
	i64 u0x5b5ba1327561f926, ; 406: lib_SkiaSharp.Views.Maui.Controls.dll.so => 218
	i64 u0x5b5f0e240a06a2a2, ; 407: da/Microsoft.Maui.Controls.resources.dll => 338
	i64 u0x5b8109e8e14c5e3e, ; 408: System.Globalization.Extensions.dll => 43
	i64 u0x5bddd04d72a9e350, ; 409: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx => 279
	i64 u0x5bdf16b09da116ab, ; 410: Xamarin.AndroidX.Collection => 254
	i64 u0x5bf46208bead7b18, ; 411: ShimSkiaSharp.dll => 214
	i64 u0x5c019d5266093159, ; 412: lib_Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll.so => 284
	i64 u0x5c30a4a35f9cc8c4, ; 413: lib_System.Reflection.Extensions.dll.so => 98
	i64 u0x5c393624b8176517, ; 414: lib_Microsoft.Extensions.Logging.dll.so => 201
	i64 u0x5c53c29f5073b0c9, ; 415: System.Diagnostics.FileVersionInfo => 30
	i64 u0x5c87463c575c7616, ; 416: lib_System.Globalization.Extensions.dll.so => 43
	i64 u0x5d0a4a29b02d9d3c, ; 417: System.Net.WebHeaderCollection.dll => 82
	i64 u0x5d40c9b15181641f, ; 418: lib_Xamarin.AndroidX.Emoji2.dll.so => 269
	i64 u0x5d6ca10d35e9485b, ; 419: lib_Xamarin.AndroidX.Concurrent.Futures.dll.so => 257
	i64 u0x5d7ec76c1c703055, ; 420: System.Threading.Tasks.Parallel => 149
	i64 u0x5db0cbbd1028510e, ; 421: lib_System.Runtime.InteropServices.dll.so => 112
	i64 u0x5db30905d3e5013b, ; 422: Xamarin.AndroidX.Collection.Jvm.dll => 255
	i64 u0x5e467bc8f09ad026, ; 423: System.Collections.Specialized.dll => 13
	i64 u0x5e5173b3208d97e7, ; 424: System.Runtime.Handles.dll => 109
	i64 u0x5ea92fdb19ec8c4c, ; 425: System.Text.Encodings.Web.dll => 141
	i64 u0x5eb8046dd40e9ac3, ; 426: System.ComponentModel.Primitives => 18
	i64 u0x5ec272d219c9aba4, ; 427: System.Security.Cryptography.Csp.dll => 126
	i64 u0x5eee1376d94c7f5e, ; 428: System.Net.HttpListener.dll => 69
	i64 u0x5f36ccf5c6a57e24, ; 429: System.Xml.ReaderWriter.dll => 162
	i64 u0x5f3bce5c22261fd2, ; 430: ExCSS.dll => 186
	i64 u0x5f4294b9b63cb842, ; 431: System.Data.Common => 24
	i64 u0x5f9a2d823f664957, ; 432: lib-el-Microsoft.Maui.Controls.resources.dll.so => 340
	i64 u0x5fa6da9c3cd8142a, ; 433: lib_Xamarin.KotlinX.Serialization.Core.dll.so => 333
	i64 u0x5fac98e0b37a5b9d, ; 434: System.Runtime.CompilerServices.Unsafe.dll => 106
	i64 u0x609f4b7b63d802d4, ; 435: lib_Microsoft.Extensions.DependencyInjection.dll.so => 196
	i64 u0x60cd4e33d7e60134, ; 436: Xamarin.KotlinX.Coroutines.Core.Jvm => 332
	i64 u0x60f62d786afcf130, ; 437: System.Memory => 66
	i64 u0x61bb78c89f867353, ; 438: System.IO => 60
	i64 u0x61be8d1299194243, ; 439: Microsoft.Maui.Controls.Xaml => 207
	i64 u0x61d2cba29557038f, ; 440: de/Microsoft.Maui.Controls.resources => 339
	i64 u0x61d88f399afb2f45, ; 441: lib_System.Runtime.Loader.dll.so => 114
	i64 u0x61ebd72849480f9a, ; 442: lib_Mapsui.dll.so => 189
	i64 u0x621c184660f54f43, ; 443: Mapsui.Nts.dll => 191
	i64 u0x622eef6f9e59068d, ; 444: System.Private.CoreLib => 179
	i64 u0x63982c87366f9be8, ; 445: Xamarin.Google.Guava => 324
	i64 u0x639fb99a7bef11de, ; 446: Xamarin.AndroidX.Navigation.Runtime.Android.dll => 297
	i64 u0x63cdbd66ac39bb46, ; 447: lib_Microsoft.VisualStudio.DesignTools.XamlTapContract.dll.so => 372
	i64 u0x63d0faf4edfc2f19, ; 448: lib-en-DalilAlHaj.resources.dll.so => 0
	i64 u0x63d5e3aa4ef9b931, ; 449: Xamarin.KotlinX.Coroutines.Android.dll => 330
	i64 u0x63f1f6883c1e23c2, ; 450: lib_System.Collections.Immutable.dll.so => 11
	i64 u0x6400f68068c1e9f1, ; 451: Xamarin.Google.Android.Material.dll => 320
	i64 u0x640e3b14dbd325c2, ; 452: System.Security.Cryptography.Algorithms.dll => 124
	i64 u0x64587004560099b9, ; 453: System.Reflection => 102
	i64 u0x64b1529a438a3c45, ; 454: lib_System.Runtime.Handles.dll.so => 109
	i64 u0x64b61dd9da8a4d57, ; 455: System.Net.ServerSentEvents.dll => 78
	i64 u0x6565fba2cd8f235b, ; 456: Xamarin.AndroidX.Lifecycle.ViewModel.Ktx => 287
	i64 u0x658f524e4aba7dad, ; 457: CommunityToolkit.Maui.dll => 182
	i64 u0x6599745d5df77d6a, ; 458: lib_Mapsui.Nts.dll.so => 191
	i64 u0x65ecac39144dd3cc, ; 459: Microsoft.Maui.Controls.dll => 206
	i64 u0x65ece51227bfa724, ; 460: lib_System.Runtime.Numerics.dll.so => 115
	i64 u0x661722438787b57f, ; 461: Xamarin.AndroidX.Annotation.Jvm.dll => 247
	i64 u0x66648161f2615420, ; 462: Mapsui.UI.Maui => 190
	i64 u0x6679b2337ee6b22a, ; 463: lib_System.IO.FileSystem.Primitives.dll.so => 51
	i64 u0x667c66a03dd97d40, ; 464: System.Linq.AsyncEnumerable.dll => 61
	i64 u0x6692e924eade1b29, ; 465: lib_System.Console.dll.so => 22
	i64 u0x66a4e5c6a3fb0bae, ; 466: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Android.dll.so => 286
	i64 u0x66d13304ce1a3efa, ; 467: Xamarin.AndroidX.CursorAdapter => 264
	i64 u0x674303f65d8fad6f, ; 468: lib_System.Net.Quic.dll.so => 75
	i64 u0x6756ca4cad62e9d6, ; 469: lib_Xamarin.AndroidX.ConstraintLayout.Core.dll.so => 259
	i64 u0x67c0802770244408, ; 470: System.Windows.dll => 160
	i64 u0x68100b69286e27cd, ; 471: lib_System.Formats.Tar.dll.so => 41
	i64 u0x68558ec653afa616, ; 472: lib-da-Microsoft.Maui.Controls.resources.dll.so => 338
	i64 u0x6872ec7a2e36b1ac, ; 473: System.Drawing.Primitives.dll => 37
	i64 u0x68fbbbe2eb455198, ; 474: System.Formats.Asn1 => 40
	i64 u0x69063fc0ba8e6bdd, ; 475: he/Microsoft.Maui.Controls.resources.dll => 344
	i64 u0x6a4d7577b2317255, ; 476: System.Runtime.InteropServices.dll => 112
	i64 u0x6abf967518ff2f5e, ; 477: fr/DalilAlHaj.resources.dll => 1
	i64 u0x6ace3b74b15ee4a4, ; 478: nb/Microsoft.Maui.Controls.resources => 353
	i64 u0x6afcedb171067e2b, ; 479: System.Core.dll => 23
	i64 u0x6bef98e124147c24, ; 480: Xamarin.Jetbrains.Annotations => 327
	i64 u0x6ce874bff138ce2b, ; 481: Xamarin.AndroidX.Lifecycle.ViewModel.dll => 285
	i64 u0x6d12bfaa99c72b1f, ; 482: lib_Microsoft.Maui.Graphics.dll.so => 210
	i64 u0x6d70755158ca866e, ; 483: lib_System.ComponentModel.EventBasedAsync.dll.so => 17
	i64 u0x6d79993361e10ef2, ; 484: Microsoft.Extensions.Primitives => 205
	i64 u0x6d7eeca99577fc8b, ; 485: lib_System.Net.WebProxy.dll.so => 83
	i64 u0x6d8515b19946b6a2, ; 486: System.Net.WebProxy.dll => 83
	i64 u0x6d86d56b84c8eb71, ; 487: lib_Xamarin.AndroidX.CursorAdapter.dll.so => 264
	i64 u0x6d9bea6b3e895cf7, ; 488: Microsoft.Extensions.Primitives.dll => 205
	i64 u0x6e25a02c3833319a, ; 489: lib_Xamarin.AndroidX.Navigation.Fragment.dll.so => 295
	i64 u0x6e79c6bd8627412a, ; 490: Xamarin.AndroidX.SavedState.SavedState.Ktx => 304
	i64 u0x6e838d9a2a6f6c9e, ; 491: lib_System.ValueTuple.dll.so => 157
	i64 u0x6e9965ce1095e60a, ; 492: lib_System.Core.dll.so => 23
	i64 u0x6f6a9cbdbc8a1853, ; 493: Mapsui.Nts => 191
	i64 u0x6fd2265da78b93a4, ; 494: lib_Microsoft.Maui.dll.so => 208
	i64 u0x6fdfc7de82c33008, ; 495: cs/Microsoft.Maui.Controls.resources => 337
	i64 u0x6ffc4967cc47ba57, ; 496: System.IO.FileSystem.Watcher.dll => 52
	i64 u0x701cd46a1c25a5fe, ; 497: System.IO.FileSystem.dll => 53
	i64 u0x709688f4ec2831e9, ; 498: lib_Xamarin.AndroidX.Media.dll.so => 292
	i64 u0x70e99f48c05cb921, ; 499: tr/Microsoft.Maui.Controls.resources.dll => 363
	i64 u0x70fd3deda22442d2, ; 500: lib-nb-Microsoft.Maui.Controls.resources.dll.so => 353
	i64 u0x71485e7ffdb4b958, ; 501: System.Reflection.Extensions => 98
	i64 u0x7162a2fce67a945f, ; 502: lib_Xamarin.Android.Glide.Annotations.dll.so => 240
	i64 u0x717530326f808838, ; 503: lib_Microsoft.Extensions.Diagnostics.Abstractions.dll.so => 198
	i64 u0x71a495ea3761dde8, ; 504: lib-it-Microsoft.Maui.Controls.resources.dll.so => 349
	i64 u0x71ad672adbe48f35, ; 505: System.ComponentModel.Primitives.dll => 18
	i64 u0x720f102581a4a5c8, ; 506: Xamarin.AndroidX.Core.ViewTree => 263
	i64 u0x725f5a9e82a45c81, ; 507: System.Security.Cryptography.Encoding => 127
	i64 u0x72b1fb4109e08d7b, ; 508: lib-hr-Microsoft.Maui.Controls.resources.dll.so => 346
	i64 u0x72e0300099accce1, ; 509: System.Xml.XPath.XDocument => 165
	i64 u0x730bfb248998f67a, ; 510: System.IO.Compression.ZipFile => 47
	i64 u0x732b2d67b9e5c47b, ; 511: Xamarin.Google.ErrorProne.Annotations.dll => 323
	i64 u0x734b76fdc0dc05bb, ; 512: lib_GoogleGson.dll.so => 187
	i64 u0x73a6be34e822f9d1, ; 513: lib_System.Runtime.Serialization.dll.so => 120
	i64 u0x73e4ce94e2eb6ffc, ; 514: lib_System.Memory.dll.so => 66
	i64 u0x743a1eccf080489a, ; 515: WindowsBase.dll => 171
	i64 u0x74e1f0dec2556118, ; 516: Mapsui.Tiling => 193
	i64 u0x755a91767330b3d4, ; 517: lib_Microsoft.Extensions.Configuration.dll.so => 194
	i64 u0x75c326eb821b85c4, ; 518: lib_System.ComponentModel.DataAnnotations.dll.so => 16
	i64 u0x76012e7334db86e5, ; 519: lib_Xamarin.AndroidX.SavedState.dll.so => 302
	i64 u0x76ca07b878f44da0, ; 520: System.Runtime.Numerics.dll => 115
	i64 u0x7736c8a96e51a061, ; 521: lib_Xamarin.AndroidX.Annotation.Jvm.dll.so => 247
	i64 u0x778a805e625329ef, ; 522: System.Linq.Parallel => 63
	i64 u0x77d9074d8f33a303, ; 523: lib_System.Net.ServerSentEvents.dll.so => 78
	i64 u0x77f8a4acc2fdc449, ; 524: System.Security.Cryptography.Cng.dll => 125
	i64 u0x780bc73597a503a9, ; 525: lib-ms-Microsoft.Maui.Controls.resources.dll.so => 352
	i64 u0x782c5d8eb99ff201, ; 526: lib_Microsoft.VisualBasic.Core.dll.so => 4
	i64 u0x783606d1e53e7a1a, ; 527: th/Microsoft.Maui.Controls.resources.dll => 362
	i64 u0x78a45e51311409b6, ; 528: Xamarin.AndroidX.Fragment.dll => 272
	i64 u0x78ed4ab8f9d800a1, ; 529: Xamarin.AndroidX.Lifecycle.ViewModel => 285
	i64 u0x79e2fd2773a12b40, ; 530: lib_CommunityToolkit.Maui.MediaElement.dll.so => 184
	i64 u0x7a5207a7c82d30b4, ; 531: lib_Xamarin.JSpecify.dll.so => 328
	i64 u0x7a7e7eddf79c5d26, ; 532: lib_Xamarin.AndroidX.Lifecycle.ViewModel.dll.so => 285
	i64 u0x7a9a57d43b0845fa, ; 533: System.AppContext => 8
	i64 u0x7ad0f4f1e5d08183, ; 534: Xamarin.AndroidX.Collection.dll => 254
	i64 u0x7adb8da2ac89b647, ; 535: fi/Microsoft.Maui.Controls.resources.dll => 342
	i64 u0x7b13d9eaa944ade8, ; 536: Xamarin.AndroidX.DynamicAnimation.dll => 268
	i64 u0x7bef86a4335c4870, ; 537: System.ComponentModel.TypeConverter => 19
	i64 u0x7c0820144cd34d6a, ; 538: sk/Microsoft.Maui.Controls.resources.dll => 360
	i64 u0x7c2a0bd1e0f988fc, ; 539: lib-de-Microsoft.Maui.Controls.resources.dll.so => 339
	i64 u0x7c41d387501568ba, ; 540: System.Net.WebClient.dll => 81
	i64 u0x7c482cd79bd24b13, ; 541: lib_Xamarin.AndroidX.ConstraintLayout.dll.so => 258
	i64 u0x7c60acf6404e96b6, ; 542: Xamarin.AndroidX.Navigation.Common.Android.dll => 294
	i64 u0x7cc637f941f716d0, ; 543: CommunityToolkit.Maui.Core => 183
	i64 u0x7cd2ec8eaf5241cd, ; 544: System.Security.dll => 135
	i64 u0x7cf9ae50dd350622, ; 545: Xamarin.Jetbrains.Annotations.dll => 327
	i64 u0x7d649b75d580bb42, ; 546: ms/Microsoft.Maui.Controls.resources.dll => 352
	i64 u0x7d8ee2bdc8e3aad1, ; 547: System.Numerics.Vectors => 87
	i64 u0x7df5df8db8eaa6ac, ; 548: Microsoft.Extensions.Logging.Debug => 203
	i64 u0x7dfc3d6d9d8d7b70, ; 549: System.Collections => 14
	i64 u0x7e2e564fa2f76c65, ; 550: lib_System.Diagnostics.Tracing.dll.so => 36
	i64 u0x7e302e110e1e1346, ; 551: lib_System.Security.Claims.dll.so => 123
	i64 u0x7e4465b3f78ad8d0, ; 552: Xamarin.KotlinX.Serialization.Core.dll => 333
	i64 u0x7e571cad5915e6c3, ; 553: lib_Xamarin.AndroidX.Lifecycle.Process.dll.so => 280
	i64 u0x7e6ac99e4e8df72f, ; 554: System.IO.Hashing => 178
	i64 u0x7e6b1ca712437d7d, ; 555: Xamarin.AndroidX.Emoji2.ViewsHelper => 270
	i64 u0x7e946809d6008ef2, ; 556: lib_System.ObjectModel.dll.so => 89
	i64 u0x7ea0272c1b4a9635, ; 557: lib_Xamarin.Android.Glide.dll.so => 239
	i64 u0x7ecc13347c8fd849, ; 558: lib_System.ComponentModel.dll.so => 20
	i64 u0x7f00ddd9b9ca5a13, ; 559: Xamarin.AndroidX.ViewPager.dll => 315
	i64 u0x7f9351cd44b1273f, ; 560: Microsoft.Extensions.Configuration.Abstractions => 195
	i64 u0x7fbd557c99b3ce6f, ; 561: lib_Xamarin.AndroidX.Lifecycle.LiveData.Core.dll.so => 278
	i64 u0x7fde679093d95532, ; 562: lib_ExoPlayer.Container.dll.so => 226
	i64 u0x8076a9a44a2ca331, ; 563: System.Net.Quic => 75
	i64 u0x808f6d3ab07f8399, ; 564: lib_NetTopologySuite.Features.dll.so => 212
	i64 u0x80b7e726b0280681, ; 565: Microsoft.VisualStudio.DesignTools.MobileTapContracts => 370
	i64 u0x80da183a87731838, ; 566: System.Reflection.Metadata => 99
	i64 u0x812c069d5cdecc17, ; 567: System.dll => 170
	i64 u0x81381be520a60adb, ; 568: Xamarin.AndroidX.Interpolator.dll => 274
	i64 u0x81657cec2b31e8aa, ; 569: System.Net => 86
	i64 u0x81ab745f6c0f5ce6, ; 570: zh-Hant/Microsoft.Maui.Controls.resources => 368
	i64 u0x8277f2be6b5ce05f, ; 571: Xamarin.AndroidX.AppCompat => 248
	i64 u0x828f06563b30bc50, ; 572: lib_Xamarin.AndroidX.CardView.dll.so => 253
	i64 u0x82b399cb01b531c4, ; 573: lib_System.Web.dll.so => 159
	i64 u0x82df8f5532a10c59, ; 574: lib_System.Drawing.dll.so => 38
	i64 u0x82f0b6e911d13535, ; 575: lib_System.Transactions.dll.so => 156
	i64 u0x82f6403342e12049, ; 576: uk/Microsoft.Maui.Controls.resources => 364
	i64 u0x83803f9f6ae5e23d, ; 577: lib_Mapsui.Tiling.dll.so => 193
	i64 u0x83c14ba66c8e2b8c, ; 578: zh-Hans/Microsoft.Maui.Controls.resources => 367
	i64 u0x842edaeb6820c0fa, ; 579: lib_Mapsui.UI.Maui.dll.so => 190
	i64 u0x846ce984efea52c7, ; 580: System.Threading.Tasks.Parallel.dll => 149
	i64 u0x84ae73148a4557d2, ; 581: lib_System.IO.Pipes.dll.so => 58
	i64 u0x84b01102c12a9232, ; 582: System.Runtime.Serialization.Json.dll => 117
	i64 u0x84f9060cc4a93c8f, ; 583: lib_SkiaSharp.dll.so => 215
	i64 u0x850c5ba0b57ce8e7, ; 584: lib_Xamarin.AndroidX.Collection.dll.so => 254
	i64 u0x851d02edd334b044, ; 585: Xamarin.AndroidX.VectorDrawable => 312
	i64 u0x85c919db62150978, ; 586: Xamarin.AndroidX.Transition.dll => 311
	i64 u0x8662aaeb94fef37f, ; 587: lib_System.Dynamic.Runtime.dll.so => 39
	i64 u0x8690556019b686eb, ; 588: Svg.Custom.dll => 220
	i64 u0x86a909228dc7657b, ; 589: lib-zh-Hant-Microsoft.Maui.Controls.resources.dll.so => 368
	i64 u0x86b3e00c36b84509, ; 590: Microsoft.Extensions.Configuration.dll => 194
	i64 u0x86b5381885cbbb52, ; 591: lib_Svg.Model.dll.so => 221
	i64 u0x86b62cb077ec4fd7, ; 592: System.Runtime.Serialization.Xml => 119
	i64 u0x86cbc49b900aeb76, ; 593: ExoPlayer => 224
	i64 u0x8706ffb12bf3f53d, ; 594: Xamarin.AndroidX.Annotation.Experimental => 246
	i64 u0x87248ce52d717bb5, ; 595: NetTopologySuite.IO.GeoJSON4STJ => 213
	i64 u0x872a5b14c18d328c, ; 596: System.ComponentModel.DataAnnotations => 16
	i64 u0x872fb9615bc2dff0, ; 597: Xamarin.Android.Glide.Annotations.dll => 240
	i64 u0x87c69b87d9283884, ; 598: lib_System.Threading.Thread.dll.so => 151
	i64 u0x87f6569b25707834, ; 599: System.IO.Compression.Brotli.dll => 45
	i64 u0x8808a9d7c53dc4c0, ; 600: lib_HarfBuzzSharp.dll.so => 188
	i64 u0x8821db2c6f223e59, ; 601: BruTile.XmlSerializers => 180
	i64 u0x8842b3a5d2d3fb36, ; 602: Microsoft.Maui.Essentials => 209
	i64 u0x88926583efe7ee86, ; 603: Xamarin.AndroidX.Activity.Ktx.dll => 244
	i64 u0x88ba6bc4f7762b03, ; 604: lib_System.Reflection.dll.so => 102
	i64 u0x88bda98e0cffb7a9, ; 605: lib_Xamarin.KotlinX.Coroutines.Core.Jvm.dll.so => 332
	i64 u0x8930322c7bd8f768, ; 606: netstandard => 173
	i64 u0x897a606c9e39c75f, ; 607: lib_System.ComponentModel.Primitives.dll.so => 18
	i64 u0x898a5c6bc9e47ec1, ; 608: lib_Xamarin.AndroidX.SavedState.SavedState.Android.dll.so => 303
	i64 u0x89911a22005b92b7, ; 609: System.IO.FileSystem.DriveInfo.dll => 50
	i64 u0x89c5188089ec2cd5, ; 610: lib_System.Runtime.InteropServices.RuntimeInformation.dll.so => 111
	i64 u0x8a19e3dc71b34b2c, ; 611: System.Reflection.TypeExtensions.dll => 101
	i64 u0x8ad229ea26432ee2, ; 612: Xamarin.AndroidX.Loader => 290
	i64 u0x8b4ff5d0fdd5faa1, ; 613: lib_System.Diagnostics.DiagnosticSource.dll.so => 29
	i64 u0x8b541d476eb3774c, ; 614: System.Security.Principal.Windows => 132
	i64 u0x8b8d01333a96d0b5, ; 615: System.Diagnostics.Process.dll => 31
	i64 u0x8b9ceca7acae3451, ; 616: lib-he-Microsoft.Maui.Controls.resources.dll.so => 344
	i64 u0x8c575135aa1ccef4, ; 617: Microsoft.Extensions.FileProviders.Abstractions => 199
	i64 u0x8cb8f612b633affb, ; 618: Xamarin.AndroidX.SavedState.SavedState.Ktx.dll => 304
	i64 u0x8cdfdb4ce85fb925, ; 619: lib_System.Security.Principal.Windows.dll.so => 132
	i64 u0x8cdfe7b8f4caa426, ; 620: System.IO.Compression.FileSystem => 46
	i64 u0x8d0f420977c2c1c7, ; 621: Xamarin.AndroidX.CursorAdapter.dll => 264
	i64 u0x8d52f7ea2796c531, ; 622: Xamarin.AndroidX.Emoji2.dll => 269
	i64 u0x8d7b8ab4b3310ead, ; 623: System.Threading => 154
	i64 u0x8da188285aadfe8e, ; 624: System.Collections.Concurrent => 10
	i64 u0x8e8f269ad1e1ff94, ; 625: lib_Xamarin.AndroidX.Tracing.Tracing.Android.dll.so => 310
	i64 u0x8ed807bfe9858dfc, ; 626: Xamarin.AndroidX.Navigation.Common => 293
	i64 u0x8ee08b8194a30f48, ; 627: lib-hi-Microsoft.Maui.Controls.resources.dll.so => 345
	i64 u0x8ef7601039857a44, ; 628: lib-ro-Microsoft.Maui.Controls.resources.dll.so => 358
	i64 u0x8f32c6f611f6ffab, ; 629: pt/Microsoft.Maui.Controls.resources.dll => 357
	i64 u0x8f44b45eb046bbd1, ; 630: System.ServiceModel.Web.dll => 136
	i64 u0x8f8829d21c8985a4, ; 631: lib-pt-BR-Microsoft.Maui.Controls.resources.dll.so => 356
	i64 u0x8fbf5b0114c6dcef, ; 632: System.Globalization.dll => 44
	i64 u0x8fcc8c2a81f3d9e7, ; 633: Xamarin.KotlinX.Serialization.Core => 333
	i64 u0x90263f8448b8f572, ; 634: lib_System.Diagnostics.TraceSource.dll.so => 35
	i64 u0x903101b46fb73a04, ; 635: _Microsoft.Android.Resource.Designer => 373
	i64 u0x90393bd4865292f3, ; 636: lib_System.IO.Compression.dll.so => 48
	i64 u0x905e2b8e7ae91ae6, ; 637: System.Threading.Tasks.Extensions.dll => 148
	i64 u0x90634f86c5ebe2b5, ; 638: Xamarin.AndroidX.Lifecycle.ViewModel.Android => 286
	i64 u0x907aee0b062c69bb, ; 639: ExoPlayer.UI => 238
	i64 u0x907b636704ad79ef, ; 640: lib_Microsoft.Maui.Controls.Xaml.dll.so => 207
	i64 u0x90e9efbfd68593e0, ; 641: lib_Xamarin.AndroidX.Lifecycle.LiveData.dll.so => 277
	i64 u0x91418dc638b29e68, ; 642: lib_Xamarin.AndroidX.CustomView.dll.so => 265
	i64 u0x9157bd523cd7ed36, ; 643: lib_System.Text.Json.dll.so => 142
	i64 u0x91a74f07b30d37e2, ; 644: System.Linq.dll => 65
	i64 u0x91cb86ea3b17111d, ; 645: System.ServiceModel.Web => 136
	i64 u0x91fa41a87223399f, ; 646: ca/Microsoft.Maui.Controls.resources.dll => 336
	i64 u0x92054e486c0c7ea7, ; 647: System.IO.FileSystem.DriveInfo => 50
	i64 u0x928614058c40c4cd, ; 648: lib_System.Xml.XPath.XDocument.dll.so => 165
	i64 u0x92b138fffca2b01e, ; 649: lib_Xamarin.AndroidX.Arch.Core.Runtime.dll.so => 251
	i64 u0x92dfc2bfc6c6a888, ; 650: Xamarin.AndroidX.Lifecycle.LiveData => 277
	i64 u0x933da2c779423d68, ; 651: Xamarin.Android.Glide.Annotations => 240
	i64 u0x9388aad9b7ae40ce, ; 652: lib_Xamarin.AndroidX.Lifecycle.Common.dll.so => 275
	i64 u0x93cfa73ab28d6e35, ; 653: ms/Microsoft.Maui.Controls.resources => 352
	i64 u0x941c00d21e5c0679, ; 654: lib_Xamarin.AndroidX.Transition.dll.so => 311
	i64 u0x944077d8ca3c6580, ; 655: System.IO.Compression.dll => 48
	i64 u0x948cffedc8ed7960, ; 656: System.Xml => 169
	i64 u0x94bbeab0d4764588, ; 657: System.IO.Hashing.dll => 178
	i64 u0x94c8990839c4bdb1, ; 658: lib_Xamarin.AndroidX.Interpolator.dll.so => 274
	i64 u0x94ec641640d81fc4, ; 659: ExoPlayer.Decoder => 231
	i64 u0x967fc325e09bfa8c, ; 660: es/Microsoft.Maui.Controls.resources => 341
	i64 u0x9686161486d34b81, ; 661: lib_Xamarin.AndroidX.ExifInterface.dll.so => 271
	i64 u0x972c18686ffdcce3, ; 662: Mapsui.UI.Maui.dll => 190
	i64 u0x9732d8dbddea3d9a, ; 663: id/Microsoft.Maui.Controls.resources => 348
	i64 u0x978be80e5210d31b, ; 664: Microsoft.Maui.Graphics.dll => 210
	i64 u0x97b8c771ea3e4220, ; 665: System.ComponentModel.dll => 20
	i64 u0x97e144c9d3c6976e, ; 666: System.Collections.Concurrent.dll => 10
	i64 u0x984184e3c70d4419, ; 667: GoogleGson => 187
	i64 u0x9843944103683dd3, ; 668: Xamarin.AndroidX.Core.Core.Ktx => 262
	i64 u0x98b05cc81e6f333c, ; 669: Xamarin.AndroidX.SavedState.SavedState.Android.dll => 303
	i64 u0x98d720cc4597562c, ; 670: System.Security.Cryptography.OpenSsl => 128
	i64 u0x991d510397f92d9d, ; 671: System.Linq.Expressions => 62
	i64 u0x996ceeb8a3da3d67, ; 672: System.Threading.Overlapped.dll => 146
	i64 u0x999cb19e1a04ffd3, ; 673: CommunityToolkit.Mvvm.dll => 185
	i64 u0x99a00ca5270c6878, ; 674: Xamarin.AndroidX.Navigation.Runtime => 296
	i64 u0x99cdc6d1f2d3a72f, ; 675: ko/Microsoft.Maui.Controls.resources.dll => 351
	i64 u0x9a01b1da98b6ee10, ; 676: Xamarin.AndroidX.Lifecycle.Runtime.dll => 281
	i64 u0x9a5ccc274fd6e6ee, ; 677: Jsr305Binding.dll => 321
	i64 u0x9a8abd23ef81c977, ; 678: NetTopologySuite.dll => 211
	i64 u0x9ae6940b11c02876, ; 679: lib_Xamarin.AndroidX.Window.dll.so => 317
	i64 u0x9b211a749105beac, ; 680: System.Transactions.Local => 155
	i64 u0x9b8734714671022d, ; 681: System.Threading.Tasks.Dataflow.dll => 147
	i64 u0x9bc6aea27fbf034f, ; 682: lib_Xamarin.KotlinX.Coroutines.Core.dll.so => 331
	i64 u0x9c244ac7cda32d26, ; 683: System.Security.Cryptography.X509Certificates.dll => 130
	i64 u0x9c3654e72f0431df, ; 684: Mapsui => 189
	i64 u0x9c465f280cf43733, ; 685: lib_Xamarin.KotlinX.Coroutines.Android.dll.so => 330
	i64 u0x9c7091f94d5e494f, ; 686: ExoPlayer.Container => 226
	i64 u0x9c8f6872beab6408, ; 687: System.Xml.XPath.XDocument.dll => 165
	i64 u0x9ce01cf91101ae23, ; 688: System.Xml.XmlDocument => 167
	i64 u0x9d128180c81d7ce6, ; 689: Xamarin.AndroidX.CustomView.PoolingContainer => 266
	i64 u0x9d5dbcf5a48583fe, ; 690: lib_Xamarin.AndroidX.Activity.dll.so => 243
	i64 u0x9d74dee1a7725f34, ; 691: Microsoft.Extensions.Configuration.Abstractions.dll => 195
	i64 u0x9dd0e195825d65c6, ; 692: lib_Xamarin.AndroidX.Navigation.Runtime.Android.dll.so => 297
	i64 u0x9e4534b6adaf6e84, ; 693: nl/Microsoft.Maui.Controls.resources => 354
	i64 u0x9e4b95dec42769f7, ; 694: System.Diagnostics.Debug.dll => 28
	i64 u0x9eaf1efdf6f7267e, ; 695: Xamarin.AndroidX.Navigation.Common.dll => 293
	i64 u0x9ef542cf1f78c506, ; 696: Xamarin.AndroidX.Lifecycle.LiveData.Core => 278
	i64 u0xa00832eb975f56a8, ; 697: lib_System.Net.dll.so => 86
	i64 u0xa0ad78236b7b267f, ; 698: Xamarin.AndroidX.Window => 317
	i64 u0xa0d8259f4cc284ec, ; 699: lib_System.Security.Cryptography.dll.so => 131
	i64 u0xa0e17ca50c77a225, ; 700: lib_Xamarin.Google.Crypto.Tink.Android.dll.so => 322
	i64 u0xa0ff9b3e34d92f11, ; 701: lib_System.Resources.Writer.dll.so => 105
	i64 u0xa12fbfb4da97d9f3, ; 702: System.Threading.Timer.dll => 153
	i64 u0xa1440773ee9d341e, ; 703: Xamarin.Google.Android.Material => 320
	i64 u0xa18c39c44cdc3465, ; 704: Xamarin.AndroidX.Window.WindowCore => 318
	i64 u0xa1b9d7c27f47219f, ; 705: Xamarin.AndroidX.Navigation.UI.dll => 298
	i64 u0xa202575b8b44335f, ; 706: en/DalilAlHaj.resources => 0
	i64 u0xa2572680829d2c7c, ; 707: System.IO.Pipelines.dll => 56
	i64 u0xa26597e57ee9c7f6, ; 708: System.Xml.XmlDocument.dll => 167
	i64 u0xa2beee74530fc01c, ; 709: SkiaSharp.Views.Android => 217
	i64 u0xa308401900e5bed3, ; 710: lib_mscorlib.dll.so => 172
	i64 u0xa395572e7da6c99d, ; 711: lib_System.Security.dll.so => 135
	i64 u0xa3e683f24b43af6f, ; 712: System.Dynamic.Runtime.dll => 39
	i64 u0xa4145becdee3dc4f, ; 713: Xamarin.AndroidX.VectorDrawable.Animated => 313
	i64 u0xa46aa1eaa214539b, ; 714: ko/Microsoft.Maui.Controls.resources => 351
	i64 u0xa4d20d2ff0563d26, ; 715: lib_CommunityToolkit.Mvvm.dll.so => 185
	i64 u0xa4edc8f2ceae241a, ; 716: System.Data.Common.dll => 24
	i64 u0xa5494f40f128ce6a, ; 717: System.Runtime.Serialization.Formatters.dll => 116
	i64 u0xa54b74df83dce92b, ; 718: System.Reflection.DispatchProxy => 94
	i64 u0xa5b7152421ed6d98, ; 719: lib_System.IO.FileSystem.Watcher.dll.so => 52
	i64 u0xa5c3844f17b822db, ; 720: lib_System.Linq.Parallel.dll.so => 63
	i64 u0xa5ce5c755bde8cb8, ; 721: lib_System.Security.Cryptography.Csp.dll.so => 126
	i64 u0xa5e599d1e0524750, ; 722: System.Numerics.Vectors.dll => 87
	i64 u0xa5f1ba49b85dd355, ; 723: System.Security.Cryptography.dll => 131
	i64 u0xa61975a5a37873ea, ; 724: lib_System.Xml.XmlSerializer.dll.so => 168
	i64 u0xa6593e21584384d2, ; 725: lib_Jsr305Binding.dll.so => 321
	i64 u0xa6645e3d03867094, ; 726: Svg.Skia => 222
	i64 u0xa66cbee0130865f7, ; 727: lib_WindowsBase.dll.so => 171
	i64 u0xa67dbee13e1df9ca, ; 728: Xamarin.AndroidX.SavedState.dll => 302
	i64 u0xa68493a85ce7fb99, ; 729: lib_ExoPlayer.dll.so => 224
	i64 u0xa684b098dd27b296, ; 730: lib_Xamarin.AndroidX.Security.SecurityCrypto.dll.so => 305
	i64 u0xa68a420042bb9b1f, ; 731: Xamarin.AndroidX.DrawerLayout.dll => 267
	i64 u0xa6d26156d1cacc7c, ; 732: Xamarin.Android.Glide.dll => 239
	i64 u0xa75386b5cb9595aa, ; 733: Xamarin.AndroidX.Lifecycle.Runtime.Android => 282
	i64 u0xa763fbb98df8d9fb, ; 734: lib_Microsoft.Win32.Primitives.dll.so => 6
	i64 u0xa78ce3745383236a, ; 735: Xamarin.AndroidX.Lifecycle.Common.Jvm => 276
	i64 u0xa7c31b56b4dc7b33, ; 736: hu/Microsoft.Maui.Controls.resources => 347
	i64 u0xa7e1d4ed4903b807, ; 737: ExoPlayer.Ext.MediaSession.dll => 234
	i64 u0xa7eab29ed44b4e7a, ; 738: Mono.Android.Export => 175
	i64 u0xa8195217cbf017b7, ; 739: Microsoft.VisualBasic.Core => 4
	i64 u0xa859a95830f367ff, ; 740: lib_Xamarin.AndroidX.Lifecycle.ViewModel.Ktx.dll.so => 287
	i64 u0xa8b52f21e0dbe690, ; 741: System.Runtime.Serialization.dll => 120
	i64 u0xa8c84ce526c2b4bd, ; 742: Microsoft.VisualStudio.DesignTools.XamlTapContract.dll => 372
	i64 u0xa8ee4ed7de2efaee, ; 743: Xamarin.AndroidX.Annotation.dll => 245
	i64 u0xa95590e7c57438a4, ; 744: System.Configuration => 21
	i64 u0xa964304b5631e28a, ; 745: CommunityToolkit.Maui.Core.dll => 183
	i64 u0xaa2219c8e3449ff5, ; 746: Microsoft.Extensions.Logging.Abstractions => 202
	i64 u0xaa443ac34067eeef, ; 747: System.Private.Xml.dll => 93
	i64 u0xaa52de307ef5d1dd, ; 748: System.Net.Http => 68
	i64 u0xaa9a7b0214a5cc5c, ; 749: System.Diagnostics.StackTrace.dll => 32
	i64 u0xaaaf86367285a918, ; 750: Microsoft.Extensions.DependencyInjection.Abstractions.dll => 197
	i64 u0xaaf84bb3f052a265, ; 751: el/Microsoft.Maui.Controls.resources => 340
	i64 u0xab9af77b5b67a0b8, ; 752: Xamarin.AndroidX.ConstraintLayout.Core => 259
	i64 u0xab9c1b2687d86b0b, ; 753: lib_System.Linq.Expressions.dll.so => 62
	i64 u0xabc67a62badb25ab, ; 754: ExoPlayer.Decoder.dll => 231
	i64 u0xac2af3fa195a15ce, ; 755: System.Runtime.Numerics => 115
	i64 u0xac5376a2a538dc10, ; 756: Xamarin.AndroidX.Lifecycle.LiveData.Core.dll => 278
	i64 u0xac5acae88f60357e, ; 757: System.Diagnostics.Tools.dll => 34
	i64 u0xac79c7e46047ad98, ; 758: System.Security.Principal.Windows.dll => 132
	i64 u0xac98d31068e24591, ; 759: System.Xml.XDocument => 164
	i64 u0xacd46e002c3ccb97, ; 760: ro/Microsoft.Maui.Controls.resources => 358
	i64 u0xacdd9e4180d56dda, ; 761: Xamarin.AndroidX.Concurrent.Futures => 257
	i64 u0xacf42eea7ef9cd12, ; 762: System.Threading.Channels => 145
	i64 u0xad263a63e0f6054b, ; 763: lib_ExoPlayer.Ext.MediaSession.dll.so => 234
	i64 u0xad89c07347f1bad6, ; 764: nl/Microsoft.Maui.Controls.resources.dll => 354
	i64 u0xadbb53caf78a79d2, ; 765: System.Web.HttpUtility => 158
	i64 u0xadc90ab061a9e6e4, ; 766: System.ComponentModel.TypeConverter.dll => 19
	i64 u0xadca1b9030b9317e, ; 767: Xamarin.AndroidX.Collection.Ktx => 256
	i64 u0xadd8eda2edf396ad, ; 768: Xamarin.Android.Glide.GifDecoder => 242
	i64 u0xadf4cf30debbeb9a, ; 769: System.Net.ServicePoint.dll => 79
	i64 u0xadf511667bef3595, ; 770: System.Net.Security => 77
	i64 u0xae0aaa94fdcfce0f, ; 771: System.ComponentModel.EventBasedAsync.dll => 17
	i64 u0xae191474c509f750, ; 772: lib_ExoPlayer.Core.dll.so => 227
	i64 u0xae282bcd03739de7, ; 773: Java.Interop => 174
	i64 u0xae53579c90db1107, ; 774: System.ObjectModel.dll => 89
	i64 u0xaf732d0b2193b8f5, ; 775: System.Security.Cryptography.OpenSsl.dll => 128
	i64 u0xafa6312f59d42642, ; 776: Mapsui.dll => 189
	i64 u0xafdb94dbccd9d11c, ; 777: Xamarin.AndroidX.Lifecycle.LiveData.dll => 277
	i64 u0xafe29f45095518e7, ; 778: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll.so => 288
	i64 u0xb03ae931fb25607e, ; 779: Xamarin.AndroidX.ConstraintLayout => 258
	i64 u0xb05cc42cd94c6d9d, ; 780: lib-sv-Microsoft.Maui.Controls.resources.dll.so => 361
	i64 u0xb0ac21bec8f428c5, ; 781: Xamarin.AndroidX.Lifecycle.Runtime.Ktx.Android.dll => 284
	i64 u0xb0bb43dc52ea59f9, ; 782: System.Diagnostics.Tracing.dll => 36
	i64 u0xb1dd05401aa8ee63, ; 783: System.Security.AccessControl => 122
	i64 u0xb220631954820169, ; 784: System.Text.RegularExpressions => 143
	i64 u0xb2376e1dbf8b4ed7, ; 785: System.Security.Cryptography.Csp => 126
	i64 u0xb24e06ce97f7b2bf, ; 786: Svg.Model.dll => 221
	i64 u0xb2a1959fe95c5402, ; 787: lib_System.Runtime.InteropServices.JavaScript.dll.so => 110
	i64 u0xb2a3f67f3bf29fce, ; 788: da/Microsoft.Maui.Controls.resources => 338
	i64 u0xb3011a0a57f7ffb2, ; 789: Microsoft.VisualStudio.DesignTools.MobileTapContracts.dll => 370
	i64 u0xb3624e8d2ac07e47, ; 790: BruTile.dll => 181
	i64 u0xb3874072ee0ecf8c, ; 791: Xamarin.AndroidX.VectorDrawable.Animated.dll => 313
	i64 u0xb3f0a0fcda8d3ebc, ; 792: Xamarin.AndroidX.CardView => 253
	i64 u0xb40bf0dc1dc5a570, ; 793: Mapsui.Rendering.Skia.dll => 192
	i64 u0xb46be1aa6d4fff93, ; 794: hi/Microsoft.Maui.Controls.resources => 345
	i64 u0xb477491be13109d8, ; 795: ar/Microsoft.Maui.Controls.resources => 335
	i64 u0xb4bd7015ecee9d86, ; 796: System.IO.Pipelines => 56
	i64 u0xb4c53d9749c5f226, ; 797: lib_System.IO.FileSystem.AccessControl.dll.so => 49
	i64 u0xb4e255db60ce6292, ; 798: ExoPlayer.Database.dll => 229
	i64 u0xb4ff710863453fda, ; 799: System.Diagnostics.FileVersionInfo.dll => 30
	i64 u0xb54092076b15e062, ; 800: System.Threading.AccessControl => 144
	i64 u0xb5c38bf497a4cfe2, ; 801: lib_System.Threading.Tasks.dll.so => 150
	i64 u0xb5c7fcdafbc67ee4, ; 802: Microsoft.Extensions.Logging.Abstractions.dll => 202
	i64 u0xb5ea31d5244c6626, ; 803: System.Threading.ThreadPool.dll => 152
	i64 u0xb7212c4683a94afe, ; 804: System.Drawing.Primitives => 37
	i64 u0xb7b7753d1f319409, ; 805: sv/Microsoft.Maui.Controls.resources => 361
	i64 u0xb81a2c6e0aee50fe, ; 806: lib_System.Private.CoreLib.dll.so => 179
	i64 u0xb8c60af47c08d4da, ; 807: System.Net.ServicePoint => 79
	i64 u0xb8e68d20aad91196, ; 808: lib_System.Xml.XPath.dll.so => 166
	i64 u0xb9185c33a1643eed, ; 809: Microsoft.CSharp.dll => 3
	i64 u0xb960d6b2200ba320, ; 810: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll => 289
	i64 u0xb97d03125eff2f3d, ; 811: ExoPlayer.Hls.dll => 233
	i64 u0xb9b8001adf4ed7cc, ; 812: lib_Xamarin.AndroidX.SlidingPaneLayout.dll.so => 306
	i64 u0xb9f64d3b230def68, ; 813: lib-pt-Microsoft.Maui.Controls.resources.dll.so => 357
	i64 u0xb9fc3c8a556e3691, ; 814: ja/Microsoft.Maui.Controls.resources => 350
	i64 u0xba4670aa94a2b3c6, ; 815: lib_System.Xml.XDocument.dll.so => 164
	i64 u0xba48785529705af9, ; 816: System.Collections.dll => 14
	i64 u0xba965b8c86359996, ; 817: lib_System.Windows.dll.so => 160
	i64 u0xbb286883bc35db36, ; 818: System.Transactions.dll => 156
	i64 u0xbb65706fde942ce3, ; 819: System.Net.Sockets => 80
	i64 u0xbba28979413cad9e, ; 820: lib_System.Runtime.CompilerServices.VisualC.dll.so => 107
	i64 u0xbbd180354b67271a, ; 821: System.Runtime.Serialization.Formatters => 116
	i64 u0xbc260cdba33291a3, ; 822: Xamarin.AndroidX.Arch.Core.Common.dll => 250
	i64 u0xbd0e2c0d55246576, ; 823: System.Net.Http.dll => 68
	i64 u0xbd3fbd85b9e1cb29, ; 824: lib_System.Net.HttpListener.dll.so => 69
	i64 u0xbd437a2cdb333d0d, ; 825: Xamarin.AndroidX.ViewPager2 => 316
	i64 u0xbd4f572d2bd0a789, ; 826: System.IO.Compression.ZipFile.dll => 47
	i64 u0xbd5d0b88d3d647a5, ; 827: lib_Xamarin.AndroidX.Browser.dll.so => 252
	i64 u0xbd877b14d0b56392, ; 828: System.Runtime.Intrinsics.dll => 113
	i64 u0xbe65a49036345cf4, ; 829: lib_System.Buffers.dll.so => 9
	i64 u0xbee38d4a88835966, ; 830: Xamarin.AndroidX.AppCompat.AppCompatResources => 249
	i64 u0xbef9919db45b4ca7, ; 831: System.IO.Pipes.AccessControl => 57
	i64 u0xbf0fa68611139208, ; 832: lib_Xamarin.AndroidX.Annotation.dll.so => 245
	i64 u0xbfc1e1fb3095f2b3, ; 833: lib_System.Net.Http.Json.dll.so => 67
	i64 u0xc040a4ab55817f58, ; 834: ar/Microsoft.Maui.Controls.resources.dll => 335
	i64 u0xc06c4a4ae0bf7076, ; 835: lib_Mapsui.Rendering.Skia.dll.so => 192
	i64 u0xc07cadab29efeba0, ; 836: Xamarin.AndroidX.Core.Core.Ktx.dll => 262
	i64 u0xc0d928351ab5ca77, ; 837: System.Console.dll => 22
	i64 u0xc0f5a221a9383aea, ; 838: System.Runtime.Intrinsics => 113
	i64 u0xc111030af54d7191, ; 839: System.Resources.Writer => 105
	i64 u0xc12b8b3afa48329c, ; 840: lib_System.Linq.dll.so => 65
	i64 u0xc183ca0b74453aa9, ; 841: lib_System.Threading.Tasks.Dataflow.dll.so => 147
	i64 u0xc1ff9ae3cdb6e1e6, ; 842: Xamarin.AndroidX.Activity.dll => 243
	i64 u0xc26c064effb1dea9, ; 843: System.Buffers.dll => 9
	i64 u0xc28c50f32f81cc73, ; 844: ja/Microsoft.Maui.Controls.resources.dll => 350
	i64 u0xc2902f6cf5452577, ; 845: lib_Mono.Android.Export.dll.so => 175
	i64 u0xc2a3bca55b573141, ; 846: System.IO.FileSystem.Watcher => 52
	i64 u0xc2bcfec99f69365e, ; 847: Xamarin.AndroidX.ViewPager2.dll => 316
	i64 u0xc30b52815b58ac2c, ; 848: lib_System.Runtime.Serialization.Xml.dll.so => 119
	i64 u0xc36d7d89c652f455, ; 849: System.Threading.Overlapped => 146
	i64 u0xc396b285e59e5493, ; 850: GoogleGson.dll => 187
	i64 u0xc3c86c1e5e12f03d, ; 851: WindowsBase => 171
	i64 u0xc40c32cd7a4745fe, ; 852: en/DalilAlHaj.resources.dll => 0
	i64 u0xc421b61fd853169d, ; 853: lib_System.Net.WebSockets.Client.dll.so => 84
	i64 u0xc463e077917aa21d, ; 854: System.Runtime.Serialization.Json => 117
	i64 u0xc48d7aeef918363b, ; 855: lib_ExoPlayer.Common.dll.so => 225
	i64 u0xc4d3858ed4d08512, ; 856: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.dll => 288
	i64 u0xc50fded0ded1418c, ; 857: lib_System.ComponentModel.TypeConverter.dll.so => 19
	i64 u0xc519125d6bc8fb11, ; 858: lib_System.Net.Requests.dll.so => 76
	i64 u0xc5293b19e4dc230e, ; 859: Xamarin.AndroidX.Navigation.Fragment => 295
	i64 u0xc5325b2fcb37446f, ; 860: lib_System.Private.Xml.dll.so => 93
	i64 u0xc535cb9a21385d9b, ; 861: lib_Xamarin.Android.Glide.DiskLruCache.dll.so => 241
	i64 u0xc5a0f4b95a699af7, ; 862: lib_System.Private.Uri.dll.so => 91
	i64 u0xc5cdcd5b6277579e, ; 863: lib_System.Security.Cryptography.Algorithms.dll.so => 124
	i64 u0xc5ec286825cb0bf4, ; 864: Xamarin.AndroidX.Tracing.Tracing => 309
	i64 u0xc6706bc8aa7fe265, ; 865: Xamarin.AndroidX.Annotation.Jvm => 247
	i64 u0xc74d70d4aa96cef3, ; 866: Xamarin.AndroidX.Navigation.Runtime.Android => 297
	i64 u0xc7c01e7d7c93a110, ; 867: System.Text.Encoding.Extensions.dll => 139
	i64 u0xc7ce851898a4548e, ; 868: lib_System.Web.HttpUtility.dll.so => 158
	i64 u0xc809d4089d2556b2, ; 869: System.Runtime.InteropServices.JavaScript.dll => 110
	i64 u0xc858a28d9ee5a6c5, ; 870: lib_System.Collections.Specialized.dll.so => 13
	i64 u0xc8ac7c6bf1c2ec51, ; 871: System.Reflection.DispatchProxy.dll => 94
	i64 u0xc8d9aa6dea6bbee3, ; 872: Mapsui.Tiling.dll => 193
	i64 u0xc9c62c8f354ac568, ; 873: lib_System.Diagnostics.TextWriterTraceListener.dll.so => 33
	i64 u0xc9e54b32fc19baf3, ; 874: lib_CommunityToolkit.Maui.dll.so => 182
	i64 u0xca3a723e7342c5b6, ; 875: lib-tr-Microsoft.Maui.Controls.resources.dll.so => 363
	i64 u0xca5801070d9fccfb, ; 876: System.Text.Encoding => 140
	i64 u0xca9d0b18cb805b89, ; 877: lib_NetTopologySuite.IO.GeoJSON4STJ.dll.so => 213
	i64 u0xcab3493c70141c2d, ; 878: pl/Microsoft.Maui.Controls.resources => 355
	i64 u0xcacfddc9f7c6de76, ; 879: ro/Microsoft.Maui.Controls.resources.dll => 358
	i64 u0xcadbc92899a777f0, ; 880: Xamarin.AndroidX.Startup.StartupRuntime => 307
	i64 u0xcba1cb79f45292b5, ; 881: Xamarin.Android.Glide.GifDecoder.dll => 242
	i64 u0xcbb5f80c7293e696, ; 882: lib_System.Globalization.Calendars.dll.so => 42
	i64 u0xcbd4fdd9cef4a294, ; 883: lib__Microsoft.Android.Resource.Designer.dll.so => 373
	i64 u0xcc15da1e07bbd994, ; 884: Xamarin.AndroidX.SlidingPaneLayout => 306
	i64 u0xcc2876b32ef2794c, ; 885: lib_System.Text.RegularExpressions.dll.so => 143
	i64 u0xcc5c3bb714c4561e, ; 886: Xamarin.KotlinX.Coroutines.Core.Jvm.dll => 332
	i64 u0xcc76886e09b88260, ; 887: Xamarin.KotlinX.Serialization.Core.Jvm.dll => 334
	i64 u0xcc9fa2923aa1c9ef, ; 888: System.Diagnostics.Contracts.dll => 27
	i64 u0xccae9bb73e2326bd, ; 889: lib_System.IO.Hashing.dll.so => 178
	i64 u0xccf25c4b634ccd3a, ; 890: zh-Hans/Microsoft.Maui.Controls.resources.dll => 367
	i64 u0xcd10a42808629144, ; 891: System.Net.Requests => 76
	i64 u0xcdca1b920e9f53ba, ; 892: Xamarin.AndroidX.Interpolator => 274
	i64 u0xcdd0c48b6937b21c, ; 893: Xamarin.AndroidX.SwipeRefreshLayout => 308
	i64 u0xcde1fa22dc303670, ; 894: Microsoft.VisualStudio.DesignTools.XamlTapContract => 372
	i64 u0xcf23d8093f3ceadf, ; 895: System.Diagnostics.DiagnosticSource.dll => 29
	i64 u0xcf5ff6b6b2c4c382, ; 896: System.Net.Mail.dll => 70
	i64 u0xcf8fc898f98b0d34, ; 897: System.Private.Xml.Linq => 92
	i64 u0xd04b5f59ed596e31, ; 898: System.Reflection.Metadata.dll => 99
	i64 u0xd063299fcfc0c93f, ; 899: lib_System.Runtime.Serialization.Json.dll.so => 117
	i64 u0xd0de8a113e976700, ; 900: System.Diagnostics.TextWriterTraceListener => 33
	i64 u0xd0fc33d5ae5d4cb8, ; 901: System.Runtime.Extensions => 108
	i64 u0xd1194e1d8a8de83c, ; 902: lib_Xamarin.AndroidX.Lifecycle.Common.Jvm.dll.so => 276
	i64 u0xd12beacdfc14f696, ; 903: System.Dynamic.Runtime => 39
	i64 u0xd16fd7fb9bbcd43e, ; 904: Microsoft.Extensions.Diagnostics.Abstractions => 198
	i64 u0xd198e7ce1b6a8344, ; 905: System.Net.Quic.dll => 75
	i64 u0xd23f43eaa6c25c64, ; 906: Xamarin.AndroidX.Media.dll => 292
	i64 u0xd304de876622896e, ; 907: ExoPlayer.Ext.MediaSession => 234
	i64 u0xd3144156a3727ebe, ; 908: Xamarin.Google.Guava.ListenableFuture => 326
	i64 u0xd333d0af9e423810, ; 909: System.Runtime.InteropServices => 112
	i64 u0xd33a415cb4278969, ; 910: System.Security.Cryptography.Encoding.dll => 127
	i64 u0xd3426d966bb704f5, ; 911: Xamarin.AndroidX.AppCompat.AppCompatResources.dll => 249
	i64 u0xd3651b6fc3125825, ; 912: System.Private.Uri.dll => 91
	i64 u0xd373685349b1fe8b, ; 913: Microsoft.Extensions.Logging.dll => 201
	i64 u0xd3801faafafb7698, ; 914: System.Private.DataContractSerialization.dll => 90
	i64 u0xd3e4c8d6a2d5d470, ; 915: it/Microsoft.Maui.Controls.resources => 349
	i64 u0xd3edcc1f25459a50, ; 916: System.Reflection.Emit => 97
	i64 u0xd4645626dffec99d, ; 917: lib_Microsoft.Extensions.DependencyInjection.Abstractions.dll.so => 197
	i64 u0xd4fa0abb79079ea9, ; 918: System.Security.Principal.dll => 133
	i64 u0xd5507e11a2b2839f, ; 919: Xamarin.AndroidX.Lifecycle.ViewModelSavedState => 288
	i64 u0xd5d04bef8478ea19, ; 920: Xamarin.AndroidX.Tracing.Tracing.dll => 309
	i64 u0xd60815f26a12e140, ; 921: Microsoft.Extensions.Logging.Debug.dll => 203
	i64 u0xd65786d27a4ad960, ; 922: lib_Microsoft.Maui.Controls.HotReload.Forms.dll.so => 369
	i64 u0xd6694f8359737e4e, ; 923: Xamarin.AndroidX.SavedState => 302
	i64 u0xd6949e129339eae5, ; 924: lib_Xamarin.AndroidX.Core.Core.Ktx.dll.so => 262
	i64 u0xd6d21782156bc35b, ; 925: Xamarin.AndroidX.SwipeRefreshLayout.dll => 308
	i64 u0xd6de019f6af72435, ; 926: Xamarin.AndroidX.ConstraintLayout.Core.dll => 259
	i64 u0xd70956d1e6deefb9, ; 927: Jsr305Binding => 321
	i64 u0xd72329819cbbbc44, ; 928: lib_Microsoft.Extensions.Configuration.Abstractions.dll.so => 195
	i64 u0xd72c760af136e863, ; 929: System.Xml.XmlSerializer.dll => 168
	i64 u0xd753f071e44c2a03, ; 930: lib_System.Security.SecureString.dll.so => 134
	i64 u0xd7b3764ada9d341d, ; 931: lib_Microsoft.Extensions.Logging.Abstractions.dll.so => 202
	i64 u0xd7f0088bc5ad71f2, ; 932: Xamarin.AndroidX.VersionedParcelable => 314
	i64 u0xd824ef6ab33f8f7a, ; 933: Xamarin.AndroidX.Window.WindowCore.dll => 318
	i64 u0xd87a3203b17af395, ; 934: ExoPlayer.Transformer.dll => 237
	i64 u0xd8fb25e28ae30a12, ; 935: Xamarin.AndroidX.ProfileInstaller.ProfileInstaller.dll => 299
	i64 u0xda1dfa4c534a9251, ; 936: Microsoft.Extensions.DependencyInjection => 196
	i64 u0xdad05a11827959a3, ; 937: System.Collections.NonGeneric.dll => 12
	i64 u0xdaefdfe71aa53cf9, ; 938: System.IO.FileSystem.Primitives => 51
	i64 u0xdb1c2595127ebf18, ; 939: ExoPlayer.Dash.dll => 228
	i64 u0xdb5383ab5865c007, ; 940: lib-vi-Microsoft.Maui.Controls.resources.dll.so => 365
	i64 u0xdb58816721c02a59, ; 941: lib_System.Reflection.Emit.ILGeneration.dll.so => 95
	i64 u0xdb8f858873e2186b, ; 942: SkiaSharp.Views.Maui.Controls => 218
	i64 u0xdbeda89f832aa805, ; 943: vi/Microsoft.Maui.Controls.resources.dll => 365
	i64 u0xdbf2a779fbc3ac31, ; 944: System.Transactions.Local.dll => 155
	i64 u0xdbf9607a441b4505, ; 945: System.Linq => 65
	i64 u0xdbfc90157a0de9b0, ; 946: lib_System.Text.Encoding.dll.so => 140
	i64 u0xdc75032002d1a212, ; 947: lib_System.Transactions.Local.dll.so => 155
	i64 u0xdca8be7403f92d4f, ; 948: lib_System.Linq.Queryable.dll.so => 64
	i64 u0xdce2c53525640bf3, ; 949: Microsoft.Extensions.Logging => 201
	i64 u0xdd2b722d78ef5f43, ; 950: System.Runtime.dll => 121
	i64 u0xdd67031857c72f96, ; 951: lib_System.Text.Encodings.Web.dll.so => 141
	i64 u0xdd70765ad6162057, ; 952: Xamarin.JSpecify => 328
	i64 u0xdd92e229ad292030, ; 953: System.Numerics.dll => 88
	i64 u0xdde30e6b77aa6f6c, ; 954: lib-zh-Hans-Microsoft.Maui.Controls.resources.dll.so => 367
	i64 u0xddf8227337aa0462, ; 955: SkiaSharp.HarfBuzz => 216
	i64 u0xde110ae80fa7c2e2, ; 956: System.Xml.XDocument.dll => 164
	i64 u0xde4726fcdf63a198, ; 957: Xamarin.AndroidX.Transition => 311
	i64 u0xde572c2b2fb32f93, ; 958: lib_System.Threading.Tasks.Extensions.dll.so => 148
	i64 u0xde8769ebda7d8647, ; 959: hr/Microsoft.Maui.Controls.resources.dll => 346
	i64 u0xdee075f3477ef6be, ; 960: Xamarin.AndroidX.ExifInterface.dll => 271
	i64 u0xdf4b773de8fb1540, ; 961: System.Net.dll => 86
	i64 u0xdf9c7682560a9629, ; 962: System.Net.ServerSentEvents => 78
	i64 u0xdfa254ebb4346068, ; 963: System.Net.Ping => 73
	i64 u0xe0142572c095a480, ; 964: Xamarin.AndroidX.AppCompat.dll => 248
	i64 u0xe021eaa401792a05, ; 965: System.Text.Encoding.dll => 140
	i64 u0xe02f89350ec78051, ; 966: Xamarin.AndroidX.CoordinatorLayout.dll => 260
	i64 u0xe0496b9d65ef5474, ; 967: Xamarin.Android.Glide.DiskLruCache.dll => 241
	i64 u0xe0f282c49dd6bd0a, ; 968: Xamarin.AndroidX.Media => 292
	i64 u0xe10b760bb1462e7a, ; 969: lib_System.Security.Cryptography.Primitives.dll.so => 129
	i64 u0xe1566bbdb759c5af, ; 970: Microsoft.Maui.Controls.HotReload.Forms.dll => 369
	i64 u0xe192a588d4410686, ; 971: lib_System.IO.Pipelines.dll.so => 56
	i64 u0xe1a08bd3fa539e0d, ; 972: System.Runtime.Loader => 114
	i64 u0xe1a77eb8831f7741, ; 973: System.Security.SecureString.dll => 134
	i64 u0xe1b52f9f816c70ef, ; 974: System.Private.Xml.Linq.dll => 92
	i64 u0xe1e199c8ab02e356, ; 975: System.Data.DataSetExtensions.dll => 25
	i64 u0xe1e7ed1a6e5f9c0e, ; 976: lib_ExoPlayer.Extractor.dll.so => 232
	i64 u0xe1ecfdb7fff86067, ; 977: System.Net.Security.dll => 77
	i64 u0xe2252a80fe853de4, ; 978: lib_System.Security.Principal.dll.so => 133
	i64 u0xe22fa4c9c645db62, ; 979: System.Diagnostics.TextWriterTraceListener.dll => 33
	i64 u0xe24095a7afddaab3, ; 980: lib_Microsoft.Extensions.Hosting.Abstractions.dll.so => 200
	i64 u0xe2420585aeceb728, ; 981: System.Net.Requests.dll => 76
	i64 u0xe26692647e6bcb62, ; 982: Xamarin.AndroidX.Lifecycle.Runtime.Ktx => 283
	i64 u0xe29b73bc11392966, ; 983: lib-id-Microsoft.Maui.Controls.resources.dll.so => 348
	i64 u0xe2ad448dee50fbdf, ; 984: System.Xml.Serialization => 163
	i64 u0xe2d920f978f5d85c, ; 985: System.Data.DataSetExtensions => 25
	i64 u0xe2e426c7714fa0bc, ; 986: Microsoft.Win32.Primitives.dll => 6
	i64 u0xe332bacb3eb4a806, ; 987: Mono.Android.Export.dll => 175
	i64 u0xe36648a5d7617a9c, ; 988: Topten.RichTextKit.dll => 223
	i64 u0xe3811d68d4fe8463, ; 989: pt-BR/Microsoft.Maui.Controls.resources.dll => 356
	i64 u0xe3b7cbae5ad66c75, ; 990: lib_System.Security.Cryptography.Encoding.dll.so => 127
	i64 u0xe4292b48f3224d5b, ; 991: lib_Xamarin.AndroidX.Core.ViewTree.dll.so => 263
	i64 u0xe494f7ced4ecd10a, ; 992: hu/Microsoft.Maui.Controls.resources.dll => 347
	i64 u0xe4a9b1e40d1e8917, ; 993: lib-fi-Microsoft.Maui.Controls.resources.dll.so => 342
	i64 u0xe4f74a0b5bf9703f, ; 994: System.Runtime.Serialization.Primitives => 118
	i64 u0xe5434e8a119ceb69, ; 995: lib_Mono.Android.dll.so => 177
	i64 u0xe55703b9ce5c038a, ; 996: System.Diagnostics.Tools => 34
	i64 u0xe57013c8afc270b5, ; 997: Microsoft.VisualBasic => 5
	i64 u0xe62913cc36bc07ec, ; 998: System.Xml.dll => 169
	i64 u0xe662f5863d169331, ; 999: ExoPlayer.Rtsp.dll => 235
	i64 u0xe6fb6bcbcc0db67a, ; 1000: lib_ExoPlayer.Rtsp.dll.so => 235
	i64 u0xe7b383d36acf3505, ; 1001: Topten.RichTextKit => 223
	i64 u0xe7bea09c4900a191, ; 1002: Xamarin.AndroidX.VectorDrawable.dll => 312
	i64 u0xe7e03cc18dcdeb49, ; 1003: lib_System.Diagnostics.StackTrace.dll.so => 32
	i64 u0xe7e147ff99a7a380, ; 1004: lib_System.Configuration.dll.so => 21
	i64 u0xe86b0df4ba9e5db8, ; 1005: lib_Xamarin.AndroidX.Lifecycle.Runtime.Android.dll.so => 282
	i64 u0xe896622fe0902957, ; 1006: System.Reflection.Emit.dll => 97
	i64 u0xe89a2a9ef110899b, ; 1007: System.Drawing.dll => 38
	i64 u0xe8c5f8c100b5934b, ; 1008: Microsoft.Win32.Registry => 7
	i64 u0xe8efe6c2171f7cd2, ; 1009: Xamarin.Google.Guava.dll => 324
	i64 u0xe93e919ce2b08636, ; 1010: lib_ExCSS.dll.so => 186
	i64 u0xe98163eb702ae5c5, ; 1011: Xamarin.AndroidX.Arch.Core.Runtime => 251
	i64 u0xe994f23ba4c143e5, ; 1012: Xamarin.KotlinX.Coroutines.Android => 330
	i64 u0xe9b9c8c0458fd92a, ; 1013: System.Windows => 160
	i64 u0xe9d166d87a7f2bdb, ; 1014: lib_Xamarin.AndroidX.Startup.StartupRuntime.dll.so => 307
	i64 u0xea5a4efc2ad81d1b, ; 1015: Xamarin.Google.ErrorProne.Annotations => 323
	i64 u0xea93a785ccb58e40, ; 1016: lib_ExoPlayer.Dash.dll.so => 228
	i64 u0xeb2313fe9d65b785, ; 1017: Xamarin.AndroidX.ConstraintLayout.dll => 258
	i64 u0xeb6e275e78cb8d42, ; 1018: Xamarin.AndroidX.LocalBroadcastManager.dll => 291
	i64 u0xed19c616b3fcb7eb, ; 1019: Xamarin.AndroidX.VersionedParcelable.dll => 314
	i64 u0xed60c6fa891c051a, ; 1020: lib_Microsoft.VisualStudio.DesignTools.TapContract.dll.so => 371
	i64 u0xedc4817167106c23, ; 1021: System.Net.Sockets.dll => 80
	i64 u0xedc632067fb20ff3, ; 1022: System.Memory.dll => 66
	i64 u0xedc8e4ca71a02a8b, ; 1023: Xamarin.AndroidX.Navigation.Runtime.dll => 296
	i64 u0xee81f5b3f1c4f83b, ; 1024: System.Threading.ThreadPool => 152
	i64 u0xeeb7ebb80150501b, ; 1025: lib_Xamarin.AndroidX.Collection.Jvm.dll.so => 255
	i64 u0xeefc635595ef57f0, ; 1026: System.Security.Cryptography.Cng => 125
	i64 u0xef03b1b5a04e9709, ; 1027: System.Text.Encoding.CodePages.dll => 138
	i64 u0xef5bcbe61622ee5f, ; 1028: Xamarin.AndroidX.Tracing.Tracing.Android.dll => 310
	i64 u0xef602c523fe2e87a, ; 1029: lib_Xamarin.Google.Guava.ListenableFuture.dll.so => 326
	i64 u0xef72742e1bcca27a, ; 1030: Microsoft.Maui.Essentials.dll => 209
	i64 u0xefd1e0c4e5c9b371, ; 1031: System.Resources.ResourceManager.dll => 104
	i64 u0xefe8f8d5ed3c72ea, ; 1032: System.Formats.Tar.dll => 41
	i64 u0xefec0b7fdc57ec42, ; 1033: Xamarin.AndroidX.Activity => 243
	i64 u0xeff59cbde4363ec3, ; 1034: System.Threading.AccessControl.dll => 144
	i64 u0xf00c29406ea45e19, ; 1035: es/Microsoft.Maui.Controls.resources.dll => 341
	i64 u0xf09e47b6ae914f6e, ; 1036: System.Net.NameResolution => 71
	i64 u0xf0ac2b489fed2e35, ; 1037: lib_System.Diagnostics.Debug.dll.so => 28
	i64 u0xf0bb49dadd3a1fe1, ; 1038: lib_System.Net.ServicePoint.dll.so => 79
	i64 u0xf0bd25bd94a833d5, ; 1039: ExoPlayer.Extractor.dll => 232
	i64 u0xf0c16dff90fbf5d6, ; 1040: Xamarin.AndroidX.Window.WindowCore.Jvm => 319
	i64 u0xf0de2537ee19c6ca, ; 1041: lib_System.Net.WebHeaderCollection.dll.so => 82
	i64 u0xf10c376a24899675, ; 1042: lib_ExoPlayer.Hls.dll.so => 233
	i64 u0xf1138779fa181c68, ; 1043: lib_Xamarin.AndroidX.Lifecycle.Runtime.dll.so => 281
	i64 u0xf11b621fc87b983f, ; 1044: Microsoft.Maui.Controls.Xaml.dll => 207
	i64 u0xf161f4f3c3b7e62c, ; 1045: System.Data => 26
	i64 u0xf16eb650d5a464bc, ; 1046: System.ValueTuple => 157
	i64 u0xf1c4b4005493d871, ; 1047: System.Formats.Asn1.dll => 40
	i64 u0xf22514cfad2d598b, ; 1048: lib_Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android.dll.so => 289
	i64 u0xf238bd79489d3a96, ; 1049: lib-nl-Microsoft.Maui.Controls.resources.dll.so => 354
	i64 u0xf260dbb5f9e31174, ; 1050: CommunityToolkit.Maui.MediaElement.dll => 184
	i64 u0xf2821428226ba02b, ; 1051: Mapsui.Rendering.Skia => 192
	i64 u0xf2feea356ba760af, ; 1052: Xamarin.AndroidX.Arch.Core.Runtime.dll => 251
	i64 u0xf300e085f8acd238, ; 1053: lib_System.ServiceProcess.dll.so => 137
	i64 u0xf34e52b26e7e059d, ; 1054: System.Runtime.CompilerServices.VisualC.dll => 107
	i64 u0xf37221fda4ef8830, ; 1055: lib_Xamarin.Google.Android.Material.dll.so => 320
	i64 u0xf3ad9b8fb3eefd12, ; 1056: lib_System.IO.UnmanagedMemoryStream.dll.so => 59
	i64 u0xf3ddfe05336abf29, ; 1057: System => 170
	i64 u0xf408654b2a135055, ; 1058: System.Reflection.Emit.ILGeneration.dll => 95
	i64 u0xf4103170a1de5bd0, ; 1059: System.Linq.Queryable.dll => 64
	i64 u0xf42d20c23173d77c, ; 1060: lib_System.ServiceModel.Web.dll.so => 136
	i64 u0xf4727d423e5d26f3, ; 1061: SkiaSharp => 215
	i64 u0xf4c1dd70a5496a17, ; 1062: System.IO.Compression => 48
	i64 u0xf4ecf4b9afc64781, ; 1063: System.ServiceProcess.dll => 137
	i64 u0xf4eeeaa566e9b970, ; 1064: lib_Xamarin.AndroidX.CustomView.PoolingContainer.dll.so => 266
	i64 u0xf518f63ead11fcd1, ; 1065: System.Threading.Tasks => 150
	i64 u0xf5fc7602fe27b333, ; 1066: System.Net.WebHeaderCollection => 82
	i64 u0xf6077741019d7428, ; 1067: Xamarin.AndroidX.CoordinatorLayout => 260
	i64 u0xf6742cbf457c450b, ; 1068: Xamarin.AndroidX.Lifecycle.Runtime.Android.dll => 282
	i64 u0xf6e8de2aebcbb422, ; 1069: lib_Xamarin.AndroidX.Window.WindowCore.Jvm.dll.so => 319
	i64 u0xf70c0a7bf8ccf5af, ; 1070: System.Web => 159
	i64 u0xf77b20923f07c667, ; 1071: de/Microsoft.Maui.Controls.resources.dll => 339
	i64 u0xf7d6224976ea4d7d, ; 1072: ExoPlayer.Extractor => 232
	i64 u0xf7e2cac4c45067b3, ; 1073: lib_System.Numerics.Vectors.dll.so => 87
	i64 u0xf7e74930e0e3d214, ; 1074: zh-HK/Microsoft.Maui.Controls.resources.dll => 366
	i64 u0xf84773b5c81e3cef, ; 1075: lib-uk-Microsoft.Maui.Controls.resources.dll.so => 364
	i64 u0xf8aac5ea82de1348, ; 1076: System.Linq.Queryable => 64
	i64 u0xf8b77539b362d3ba, ; 1077: lib_System.Reflection.Primitives.dll.so => 100
	i64 u0xf8e045dc345b2ea3, ; 1078: lib_Xamarin.AndroidX.RecyclerView.dll.so => 300
	i64 u0xf90ee150b8b65f69, ; 1079: NetTopologySuite.Features.dll => 212
	i64 u0xf915dc29808193a1, ; 1080: System.Web.HttpUtility.dll => 158
	i64 u0xf96c777a2a0686f4, ; 1081: hi/Microsoft.Maui.Controls.resources.dll => 345
	i64 u0xf9be54c8bcf8ff3b, ; 1082: System.Security.AccessControl.dll => 122
	i64 u0xf9eec5bb3a6aedc6, ; 1083: Microsoft.Extensions.Options => 204
	i64 u0xfa0e82300e67f913, ; 1084: lib_System.AppContext.dll.so => 8
	i64 u0xfa2fdb27e8a2c8e8, ; 1085: System.ComponentModel.EventBasedAsync => 17
	i64 u0xfa3f278f288b0e84, ; 1086: lib_System.Net.Security.dll.so => 77
	i64 u0xfa504dfa0f097d72, ; 1087: Microsoft.Extensions.FileProviders.Abstractions.dll => 199
	i64 u0xfa5754ddda6c4b4e, ; 1088: lib_ExoPlayer.Decoder.dll.so => 231
	i64 u0xfa5ed7226d978949, ; 1089: lib-ar-Microsoft.Maui.Controls.resources.dll.so => 335
	i64 u0xfa645d91e9fc4cba, ; 1090: System.Threading.Thread => 151
	i64 u0xfa96f06b9953ab91, ; 1091: lib_ExoPlayer.Transformer.dll.so => 237
	i64 u0xfa99d44ebf9bea5b, ; 1092: SkiaSharp.Views.Maui.Core => 219
	i64 u0xfad4d2c770e827f9, ; 1093: lib_System.IO.IsolatedStorage.dll.so => 54
	i64 u0xfb06dd2338e6f7c4, ; 1094: System.Net.Ping.dll => 73
	i64 u0xfb087abe5365e3b7, ; 1095: lib_System.Data.DataSetExtensions.dll.so => 25
	i64 u0xfb846e949baff5ea, ; 1096: System.Xml.Serialization.dll => 163
	i64 u0xfbad3e4ce4b98145, ; 1097: System.Security.Cryptography.X509Certificates => 130
	i64 u0xfbf0a31c9fc34bc4, ; 1098: lib_System.Net.Http.dll.so => 68
	i64 u0xfc0ee5ac47a00750, ; 1099: ExCSS => 186
	i64 u0xfc61ddcf78dd1f54, ; 1100: Xamarin.AndroidX.LocalBroadcastManager => 291
	i64 u0xfc6b7527cc280b3f, ; 1101: lib_System.Runtime.Serialization.Formatters.dll.so => 116
	i64 u0xfc719aec26adf9d9, ; 1102: Xamarin.AndroidX.Navigation.Fragment.dll => 295
	i64 u0xfc82690c2fe2735c, ; 1103: Xamarin.AndroidX.Lifecycle.Process.dll => 280
	i64 u0xfc93fc307d279893, ; 1104: System.IO.Pipes.AccessControl.dll => 57
	i64 u0xfcd302092ada6328, ; 1105: System.IO.MemoryMappedFiles.dll => 55
	i64 u0xfd22f00870e40ae0, ; 1106: lib_Xamarin.AndroidX.DrawerLayout.dll.so => 267
	i64 u0xfd49b3c1a76e2748, ; 1107: System.Runtime.InteropServices.RuntimeInformation => 111
	i64 u0xfd50b45aad5dfa23, ; 1108: lib_ExoPlayer.SmoothStreaming.dll.so => 236
	i64 u0xfd536c702f64dc47, ; 1109: System.Text.Encoding.Extensions => 139
	i64 u0xfd583f7657b6a1cb, ; 1110: Xamarin.AndroidX.Fragment => 272
	i64 u0xfd8dd91a2c26bd5d, ; 1111: Xamarin.AndroidX.Lifecycle.Runtime => 281
	i64 u0xfda36abccf05cf5c, ; 1112: System.Net.WebSockets.Client => 84
	i64 u0xfdbe4710aa9beeff, ; 1113: CommunityToolkit.Maui => 182
	i64 u0xfddbe9695626a7f5, ; 1114: Xamarin.AndroidX.Lifecycle.Common => 275
	i64 u0xfe30a99332f3d503, ; 1115: CommunityToolkit.Maui.MediaElement => 184
	i64 u0xfeae9952cf03b8cb, ; 1116: tr/Microsoft.Maui.Controls.resources => 363
	i64 u0xfebe1950717515f9, ; 1117: Xamarin.AndroidX.Lifecycle.LiveData.Core.Ktx.dll => 279
	i64 u0xfeca84fe7f34860b, ; 1118: HarfBuzzSharp.dll => 188
	i64 u0xff270a55858bac8d, ; 1119: System.Security.Principal => 133
	i64 u0xff9b54613e0d2cc8, ; 1120: System.Net.Http.Json => 67
	i64 u0xffdb7a971be4ec73 ; 1121: System.ValueTuple.dll => 157
], align 8

@assembly_image_cache_indices = dso_local local_unnamed_addr constant [1122 x i32] [
	i32 44, i32 331, i32 211, i32 308, i32 15, i32 238, i32 180, i32 296,
	i32 183, i32 109, i32 176, i32 50, i32 248, i32 9, i32 90, i32 359,
	i32 337, i32 365, i32 268, i32 233, i32 74, i32 1, i32 300, i32 14,
	i32 208, i32 222, i32 106, i32 188, i32 366, i32 161, i32 21, i32 273,
	i32 255, i32 166, i32 270, i32 312, i32 172, i32 225, i32 359, i32 12,
	i32 203, i32 313, i32 100, i32 266, i32 267, i32 15, i32 204, i32 12,
	i32 131, i32 99, i32 216, i32 145, i32 227, i32 41, i32 360, i32 334,
	i32 315, i32 356, i32 177, i32 242, i32 7, i32 209, i32 70, i32 305,
	i32 214, i32 134, i32 221, i32 304, i32 269, i32 71, i32 217, i32 2,
	i32 256, i32 230, i32 69, i32 59, i32 265, i32 54, i32 45, i32 129,
	i32 70, i32 85, i32 283, i32 371, i32 163, i32 96, i32 103, i32 300,
	i32 146, i32 236, i32 156, i32 252, i32 343, i32 167, i32 174, i32 344,
	i32 197, i32 85, i32 371, i32 328, i32 256, i32 6, i32 7, i32 53,
	i32 105, i32 227, i32 225, i32 58, i32 1, i32 124, i32 102, i32 173,
	i32 122, i32 331, i32 23, i32 347, i32 141, i32 101, i32 334, i32 213,
	i32 81, i32 353, i32 307, i32 123, i32 10, i32 170, i32 362, i32 73,
	i32 241, i32 284, i32 301, i32 176, i32 150, i32 42, i32 305, i32 49,
	i32 32, i32 325, i32 224, i32 298, i32 351, i32 149, i32 204, i32 168,
	i32 30, i32 88, i32 309, i32 81, i32 45, i32 237, i32 228, i32 31,
	i32 44, i32 107, i32 121, i32 246, i32 47, i32 95, i32 362, i32 58,
	i32 153, i32 370, i32 151, i32 104, i32 51, i32 236, i32 22, i32 261,
	i32 118, i32 239, i32 343, i32 322, i32 329, i32 205, i32 98, i32 60,
	i32 348, i32 346, i32 318, i32 85, i32 322, i32 174, i32 28, i32 74,
	i32 299, i32 198, i32 215, i32 271, i32 294, i32 369, i32 364, i32 72,
	i32 216, i32 35, i32 342, i32 16, i32 143, i32 40, i32 368, i32 257,
	i32 355, i32 138, i32 96, i32 92, i32 154, i32 325, i32 319, i32 361,
	i32 26, i32 142, i32 59, i32 144, i32 53, i32 340, i32 31, i32 162,
	i32 36, i32 169, i32 272, i32 54, i32 373, i32 317, i32 94, i32 180,
	i32 253, i32 37, i32 218, i32 343, i32 162, i32 11, i32 341, i32 80,
	i32 223, i32 61, i32 57, i32 208, i32 337, i32 206, i32 15, i32 316,
	i32 194, i32 250, i32 113, i32 287, i32 214, i32 34, i32 108, i32 88,
	i32 96, i32 55, i32 100, i32 327, i32 60, i32 11, i32 106, i32 265,
	i32 71, i32 212, i32 315, i32 336, i32 199, i32 129, i32 301, i32 120,
	i32 2, i32 139, i32 303, i32 130, i32 110, i32 329, i32 135, i32 252,
	i32 326, i32 152, i32 181, i32 161, i32 273, i32 261, i32 268, i32 2,
	i32 301, i32 101, i32 26, i32 306, i32 148, i32 200, i32 293, i32 219,
	i32 5, i32 211, i32 172, i32 249, i32 104, i32 166, i32 103, i32 263,
	i32 27, i32 97, i32 173, i32 177, i32 244, i32 5, i32 355, i32 310,
	i32 270, i32 3, i32 118, i32 329, i32 273, i32 280, i32 220, i32 35,
	i32 220, i32 8, i32 359, i32 161, i32 357, i32 229, i32 55, i32 89,
	i32 314, i32 298, i32 46, i32 279, i32 108, i32 229, i32 49, i32 142,
	i32 217, i32 289, i32 226, i32 67, i32 290, i32 72, i32 84, i32 222,
	i32 62, i32 93, i32 159, i32 250, i32 137, i32 114, i32 61, i32 349,
	i32 290, i32 230, i32 299, i32 176, i32 138, i32 145, i32 42, i32 336,
	i32 206, i32 63, i32 185, i32 286, i32 83, i32 27, i32 38, i32 103,
	i32 283, i32 74, i32 24, i32 261, i32 210, i32 360, i32 238, i32 125,
	i32 72, i32 111, i32 366, i32 291, i32 123, i32 121, i32 275, i32 294,
	i32 230, i32 276, i32 13, i32 4, i32 128, i32 219, i32 181, i32 119,
	i32 147, i32 43, i32 91, i32 325, i32 245, i32 179, i32 200, i32 29,
	i32 153, i32 350, i32 196, i32 323, i32 244, i32 3, i32 235, i32 246,
	i32 46, i32 260, i32 154, i32 20, i32 324, i32 90, i32 218, i32 338,
	i32 43, i32 279, i32 254, i32 214, i32 284, i32 98, i32 201, i32 30,
	i32 43, i32 82, i32 269, i32 257, i32 149, i32 112, i32 255, i32 13,
	i32 109, i32 141, i32 18, i32 126, i32 69, i32 162, i32 186, i32 24,
	i32 340, i32 333, i32 106, i32 196, i32 332, i32 66, i32 60, i32 207,
	i32 339, i32 114, i32 189, i32 191, i32 179, i32 324, i32 297, i32 372,
	i32 0, i32 330, i32 11, i32 320, i32 124, i32 102, i32 109, i32 78,
	i32 287, i32 182, i32 191, i32 206, i32 115, i32 247, i32 190, i32 51,
	i32 61, i32 22, i32 286, i32 264, i32 75, i32 259, i32 160, i32 41,
	i32 338, i32 37, i32 40, i32 344, i32 112, i32 1, i32 353, i32 23,
	i32 327, i32 285, i32 210, i32 17, i32 205, i32 83, i32 83, i32 264,
	i32 205, i32 295, i32 304, i32 157, i32 23, i32 191, i32 208, i32 337,
	i32 52, i32 53, i32 292, i32 363, i32 353, i32 98, i32 240, i32 198,
	i32 349, i32 18, i32 263, i32 127, i32 346, i32 165, i32 47, i32 323,
	i32 187, i32 120, i32 66, i32 171, i32 193, i32 194, i32 16, i32 302,
	i32 115, i32 247, i32 63, i32 78, i32 125, i32 352, i32 4, i32 362,
	i32 272, i32 285, i32 184, i32 328, i32 285, i32 8, i32 254, i32 342,
	i32 268, i32 19, i32 360, i32 339, i32 81, i32 258, i32 294, i32 183,
	i32 135, i32 327, i32 352, i32 87, i32 203, i32 14, i32 36, i32 123,
	i32 333, i32 280, i32 178, i32 270, i32 89, i32 239, i32 20, i32 315,
	i32 195, i32 278, i32 226, i32 75, i32 212, i32 370, i32 99, i32 170,
	i32 274, i32 86, i32 368, i32 248, i32 253, i32 159, i32 38, i32 156,
	i32 364, i32 193, i32 367, i32 190, i32 149, i32 58, i32 117, i32 215,
	i32 254, i32 312, i32 311, i32 39, i32 220, i32 368, i32 194, i32 221,
	i32 119, i32 224, i32 246, i32 213, i32 16, i32 240, i32 151, i32 45,
	i32 188, i32 180, i32 209, i32 244, i32 102, i32 332, i32 173, i32 18,
	i32 303, i32 50, i32 111, i32 101, i32 290, i32 29, i32 132, i32 31,
	i32 344, i32 199, i32 304, i32 132, i32 46, i32 264, i32 269, i32 154,
	i32 10, i32 310, i32 293, i32 345, i32 358, i32 357, i32 136, i32 356,
	i32 44, i32 333, i32 35, i32 373, i32 48, i32 148, i32 286, i32 238,
	i32 207, i32 277, i32 265, i32 142, i32 65, i32 136, i32 336, i32 50,
	i32 165, i32 251, i32 277, i32 240, i32 275, i32 352, i32 311, i32 48,
	i32 169, i32 178, i32 274, i32 231, i32 341, i32 271, i32 190, i32 348,
	i32 210, i32 20, i32 10, i32 187, i32 262, i32 303, i32 128, i32 62,
	i32 146, i32 185, i32 296, i32 351, i32 281, i32 321, i32 211, i32 317,
	i32 155, i32 147, i32 331, i32 130, i32 189, i32 330, i32 226, i32 165,
	i32 167, i32 266, i32 243, i32 195, i32 297, i32 354, i32 28, i32 293,
	i32 278, i32 86, i32 317, i32 131, i32 322, i32 105, i32 153, i32 320,
	i32 318, i32 298, i32 0, i32 56, i32 167, i32 217, i32 172, i32 135,
	i32 39, i32 313, i32 351, i32 185, i32 24, i32 116, i32 94, i32 52,
	i32 63, i32 126, i32 87, i32 131, i32 168, i32 321, i32 222, i32 171,
	i32 302, i32 224, i32 305, i32 267, i32 239, i32 282, i32 6, i32 276,
	i32 347, i32 234, i32 175, i32 4, i32 287, i32 120, i32 372, i32 245,
	i32 21, i32 183, i32 202, i32 93, i32 68, i32 32, i32 197, i32 340,
	i32 259, i32 62, i32 231, i32 115, i32 278, i32 34, i32 132, i32 164,
	i32 358, i32 257, i32 145, i32 234, i32 354, i32 158, i32 19, i32 256,
	i32 242, i32 79, i32 77, i32 17, i32 227, i32 174, i32 89, i32 128,
	i32 189, i32 277, i32 288, i32 258, i32 361, i32 284, i32 36, i32 122,
	i32 143, i32 126, i32 221, i32 110, i32 338, i32 370, i32 181, i32 313,
	i32 253, i32 192, i32 345, i32 335, i32 56, i32 49, i32 229, i32 30,
	i32 144, i32 150, i32 202, i32 152, i32 37, i32 361, i32 179, i32 79,
	i32 166, i32 3, i32 289, i32 233, i32 306, i32 357, i32 350, i32 164,
	i32 14, i32 160, i32 156, i32 80, i32 107, i32 116, i32 250, i32 68,
	i32 69, i32 316, i32 47, i32 252, i32 113, i32 9, i32 249, i32 57,
	i32 245, i32 67, i32 335, i32 192, i32 262, i32 22, i32 113, i32 105,
	i32 65, i32 147, i32 243, i32 9, i32 350, i32 175, i32 52, i32 316,
	i32 119, i32 146, i32 187, i32 171, i32 0, i32 84, i32 117, i32 225,
	i32 288, i32 19, i32 76, i32 295, i32 93, i32 241, i32 91, i32 124,
	i32 309, i32 247, i32 297, i32 139, i32 158, i32 110, i32 13, i32 94,
	i32 193, i32 33, i32 182, i32 363, i32 140, i32 213, i32 355, i32 358,
	i32 307, i32 242, i32 42, i32 373, i32 306, i32 143, i32 332, i32 334,
	i32 27, i32 178, i32 367, i32 76, i32 274, i32 308, i32 372, i32 29,
	i32 70, i32 92, i32 99, i32 117, i32 33, i32 108, i32 276, i32 39,
	i32 198, i32 75, i32 292, i32 234, i32 326, i32 112, i32 127, i32 249,
	i32 91, i32 201, i32 90, i32 349, i32 97, i32 197, i32 133, i32 288,
	i32 309, i32 203, i32 369, i32 302, i32 262, i32 308, i32 259, i32 321,
	i32 195, i32 168, i32 134, i32 202, i32 314, i32 318, i32 237, i32 299,
	i32 196, i32 12, i32 51, i32 228, i32 365, i32 95, i32 218, i32 365,
	i32 155, i32 65, i32 140, i32 155, i32 64, i32 201, i32 121, i32 141,
	i32 328, i32 88, i32 367, i32 216, i32 164, i32 311, i32 148, i32 346,
	i32 271, i32 86, i32 78, i32 73, i32 248, i32 140, i32 260, i32 241,
	i32 292, i32 129, i32 369, i32 56, i32 114, i32 134, i32 92, i32 25,
	i32 232, i32 77, i32 133, i32 33, i32 200, i32 76, i32 283, i32 348,
	i32 163, i32 25, i32 6, i32 175, i32 223, i32 356, i32 127, i32 263,
	i32 347, i32 342, i32 118, i32 177, i32 34, i32 5, i32 169, i32 235,
	i32 235, i32 223, i32 312, i32 32, i32 21, i32 282, i32 97, i32 38,
	i32 7, i32 324, i32 186, i32 251, i32 330, i32 160, i32 307, i32 323,
	i32 228, i32 258, i32 291, i32 314, i32 371, i32 80, i32 66, i32 296,
	i32 152, i32 255, i32 125, i32 138, i32 310, i32 326, i32 209, i32 104,
	i32 41, i32 243, i32 144, i32 341, i32 71, i32 28, i32 79, i32 232,
	i32 319, i32 82, i32 233, i32 281, i32 207, i32 26, i32 157, i32 40,
	i32 289, i32 354, i32 184, i32 192, i32 251, i32 137, i32 107, i32 320,
	i32 59, i32 170, i32 95, i32 64, i32 136, i32 215, i32 48, i32 137,
	i32 266, i32 150, i32 82, i32 260, i32 282, i32 319, i32 159, i32 339,
	i32 232, i32 87, i32 366, i32 364, i32 64, i32 100, i32 300, i32 212,
	i32 158, i32 345, i32 122, i32 204, i32 8, i32 17, i32 77, i32 199,
	i32 231, i32 335, i32 151, i32 237, i32 219, i32 54, i32 73, i32 25,
	i32 163, i32 130, i32 68, i32 186, i32 291, i32 116, i32 295, i32 280,
	i32 57, i32 55, i32 267, i32 111, i32 236, i32 139, i32 272, i32 281,
	i32 84, i32 182, i32 275, i32 184, i32 363, i32 279, i32 188, i32 133,
	i32 67, i32 157
], align 4

@marshal_methods_number_of_classes = dso_local local_unnamed_addr constant i32 0, align 4

@marshal_methods_class_cache = dso_local local_unnamed_addr global [0 x %struct.MarshalMethodsManagedClass] zeroinitializer, align 8

; Names of classes in which marshal methods reside
@mm_class_names = dso_local local_unnamed_addr constant [0 x ptr] zeroinitializer, align 8

@mm_method_names = dso_local local_unnamed_addr constant [1 x %struct.MarshalMethodName] [
	%struct.MarshalMethodName {
		i64 u0x0000000000000000, ; name: 
		ptr @.MarshalMethodName.0_name; char* name
	} ; 0
], align 8

; get_function_pointer (uint32_t mono_image_index, uint32_t class_index, uint32_t method_token, void*& target_ptr)
@get_function_pointer = internal dso_local unnamed_addr global ptr null, align 8

; Functions

; Function attributes: memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" uwtable willreturn
define void @xamarin_app_init(ptr nocapture noundef readnone %env, ptr noundef %fn) local_unnamed_addr #0
{
	%fnIsNull = icmp eq ptr %fn, null
	br i1 %fnIsNull, label %1, label %2

1: ; preds = %0
	%putsResult = call noundef i32 @puts(ptr @.mm.0)
	call void @abort()
	unreachable 

2: ; preds = %1, %0
	store ptr %fn, ptr @get_function_pointer, align 8, !tbaa !3
	ret void
}

; Strings
@.mm.0 = private unnamed_addr constant [40 x i8] c"get_function_pointer MUST be specified\0A\00", align 1

;MarshalMethodName
@.MarshalMethodName.0_name = private unnamed_addr constant [1 x i8] c"\00", align 1

; External functions

; Function attributes: noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8"
declare void @abort() local_unnamed_addr #2

; Function attributes: nofree nounwind
declare noundef i32 @puts(ptr noundef) local_unnamed_addr #1
attributes #0 = { memory(write, argmem: none, inaccessiblemem: none) "min-legal-vector-width"="0" mustprogress nofree norecurse nosync "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" uwtable willreturn }
attributes #1 = { nofree nounwind }
attributes #2 = { noreturn "no-trapping-math"="true" nounwind "stack-protector-buffer-size"="8" "target-cpu"="generic" "target-features"="+fix-cortex-a53-835769,+neon,+outline-atomics,+v8a" }

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/10.0.1xx @ 01024bb616e7b80417a2c6d320885bfdb956f20a"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
