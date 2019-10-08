using System.Reflection;
using System.Runtime.InteropServices;
using Vintagestory.API.Common;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Graveyard")]
[assembly: AssemblyDescription("")]
//[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("")]

[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: Guid("203DFBF1-3599-43FD-8487-E1C79C2B788F")]

[assembly: AssemblyVersion("1.1.4")]

[assembly: ModInfo( "graveyard", "graveyard",
    Version = "1.1.4",
    Description = "Places a gravestone when you die, the gravestone stores your items and you can get them by right clicking in the block",
    Authors = new[] { "cout970" })]

[assembly: ModDependency("game")]