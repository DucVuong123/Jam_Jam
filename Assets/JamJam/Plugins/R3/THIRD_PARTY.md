# Third-party dependencies

| Component | Version | Source | License |
|---|---|---|---|
| R3 | 1.3.1 | https://www.nuget.org/packages/R3/1.3.1 | MIT, LICENSE-R3.txt |
| R3.Unity | 1.3.1 | https://github.com/Cysharp/R3/tree/1.3.1/src/R3.Unity/Assets/R3.Unity | MIT, LICENSE-R3.txt |
| Microsoft.Bcl.TimeProvider | 8.0.0 | https://www.nuget.org/packages/Microsoft.Bcl.TimeProvider/8.0.0 | MIT, LICENSE-DOTNET.txt |
| Microsoft.Bcl.AsyncInterfaces | 6.0.0 | https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/6.0.0 | MIT, LICENSE-DOTNET.txt |
| System.Threading.Channels | 8.0.0 | https://www.nuget.org/packages/System.Threading.Channels/8.0.0 | MIT, LICENSE-DOTNET.txt |
| System.ComponentModel.Annotations | 5.0.0 | https://www.nuget.org/packages/System.ComponentModel.Annotations/5.0.0 | MIT, LICENSE-DOTNET.txt |

R3, AsyncInterfaces, Channels and Annotations use netstandard2.1 assemblies. TimeProvider uses netstandard2.0. R3.Unity is restored by Unity Package Manager from the pinned Git tag. No Unity installation DLL is modified.
