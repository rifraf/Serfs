using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Serfs")]
[assembly: AssemblyDescription("Simple Embedded Resource File System. http://github.com/rifraf/Serfs")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#elif BETA
[assembly: AssemblyConfiguration("Beta")]
#else
[assembly: AssemblyConfiguration("")]
#endif
[assembly: AssemblyCompany("djlSoft")]
[assembly: AssemblyProduct("Serfs")]
[assembly: AssemblyCopyright("Copyright David Lake © 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("91cc985f-1a81-4ca4-a777-b08b4b88bb7f")]

[assembly: AssemblyVersion("0.1.0.$WCREV$")]
[assembly: AssemblyFileVersion("0.1.0.$WCREV$")]
[assembly: AssemblyInformationalVersionAttribute("0.1.0.$WCREV$")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, Execution = true)]