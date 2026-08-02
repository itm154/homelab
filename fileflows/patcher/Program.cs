using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace FileFlowsPatcher
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: patcher <path-to-FileFlows.Services.dll>");
                return 1;
            }

            string dllPath = Path.GetFullPath(args[0]);
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"Error: File not found at '{dllPath}'");
                return 1;
            }

            string binDir = Path.GetDirectoryName(dllPath) ?? "";
            string commonDllPath = Path.Combine(binDir, "FileFlows.Common.dll");
            if (!File.Exists(commonDllPath))
            {
                Console.WriteLine($"Error: FileFlows.Common.dll not found at '{commonDllPath}'");
                return 1;
            }

            try
            {
                Console.WriteLine($"Loading {dllPath}...");
                using var module = ModuleDefMD.Load(dllPath);

                Console.WriteLine($"Loading {commonDllPath}...");
                using var commonModule = ModuleDefMD.Load(commonDllPath);

                var licenseServiceType = module.Find("FileFlows.Helpers.LicenseService", true);
                if (licenseServiceType == null)
                {
                    Console.WriteLine("Error: Type FileFlows.Helpers.LicenseService not found in FileFlows.Services.dll!");
                    return 1;
                }

                var licenseType = commonModule.Find("FileFlows.Common.License", true);
                if (licenseType == null)
                {
                    Console.WriteLine("Error: Type FileFlows.Common.License not found in FileFlows.Common.dll!");
                    return 1;
                }

                // Patch IsLicensed methods to return true
                foreach (var method in licenseServiceType.Methods.Where(m => m.Name == "IsLicensed"))
                {
                    Console.WriteLine($"Patching {method.FullName} to return true");
                    method.Body = new CilBody();
                    method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
                    method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                }

                // Patch GetLicensedNodes and GetLicensedProcessingNodes to return 100
                var nodeMethods = licenseServiceType.Methods.Where(m => m.Name == "GetLicensedNodes" || m.Name == "GetLicensedProcessingNodes");
                foreach (var method in nodeMethods)
                {
                    Console.WriteLine($"Patching {method.FullName} to return 100");
                    method.Body = new CilBody();
                    method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(100));
                    method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                }

                // Patch Update method to return Task.CompletedTask
                var updateMethod = licenseServiceType.Methods.FirstOrDefault(m => m.Name == "Update");
                if (updateMethod != null)
                {
                    Console.WriteLine($"Patching {updateMethod.FullName} to return Task.CompletedTask");
                    var getCompletedTask = module.Import(typeof(System.Threading.Tasks.Task).GetProperty("CompletedTask")!.GetMethod);
                    updateMethod.Body = new CilBody();
                    updateMethod.Body.Instructions.Add(OpCodes.Call.ToInstruction(getCompletedTask));
                    updateMethod.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                }

                // Patch GetLicense method to construct and return a valid Enterprise License
                var getLicenseMethod = licenseServiceType.Methods.FirstOrDefault(m => m.Name == "GetLicense");
                if (getLicenseMethod != null)
                {
                    Console.WriteLine($"Patching {getLicenseMethod.FullName} to return valid Enterprise License");

                    // Import License type, constructor, and property setters
                    var licenseTypeRef = module.Import(licenseType);
                    var ctor = licenseType.FindDefaultConstructor();
                    var ctorRef = module.Import(ctor);

                    var setStatusRef = module.Import(licenseType.FindMethod("set_Status"));
                    var setLevelRef = module.Import(licenseType.FindMethod("set_Level"));
                    var setFlagsRef = module.Import(licenseType.FindMethod("set_Flags"));
                    var setExpirationRef = module.Import(licenseType.FindMethod("set_ExpirationDateUtc"));
                    var setNodesRef = module.Import(licenseType.FindMethod("set_ProcessingNodes"));
                    var setUsersRef = module.Import(licenseType.FindMethod("set_FileDropUsers"));
                    var setFilesRef = module.Import(licenseType.FindMethod("set_Files"));

                    // Resolve enum values dynamically
                    var licenseStatusType = commonModule.Find("FileFlows.Common.LicenseStatus", true);
                    int validStatusVal = GetEnumValue(licenseStatusType, "Valid");

                    var licenseLevelType = commonModule.Find("FileFlows.Common.LicenseLevel", true);
                    int enterpriseLevelVal = GetEnumValue(licenseLevelType, "Enterprise");

                    // Expiration date helper
                    var maxValueField = module.Import(typeof(DateTime).GetField("MaxValue")!);

                    var body = new CilBody();
                    
                    // 1. Create License instance
                    body.Instructions.Add(OpCodes.Newobj.ToInstruction(ctorRef));

                    // 2. Set Status = Valid
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(validStatusVal));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setStatusRef));

                    // 3. Set Level = Enterprise
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(enterpriseLevelVal));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setLevelRef));

                    // 4. Set Flags = int.MaxValue (all flags enabled)
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(2147483647));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setFlagsRef));

                    // 5. Set ExpirationDateUtc = DateTime.MaxValue
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldsfld.ToInstruction(maxValueField));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setExpirationRef));

                    // 6. Set ProcessingNodes = 100
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(100));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setNodesRef));

                    // 7. Set FileDropUsers = 100
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(100));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setUsersRef));

                    // 8. Set Files = int.MaxValue
                    body.Instructions.Add(OpCodes.Dup.ToInstruction());
                    body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(2147483647));
                    body.Instructions.Add(OpCodes.Callvirt.ToInstruction(setFilesRef));

                    // 9. Return the License instance
                    body.Instructions.Add(OpCodes.Ret.ToInstruction());

                    getLicenseMethod.Body = body;
                }

                // Write patched DLL back to disk
                Console.WriteLine("Saving patched assembly...");
                string tempOutput = dllPath + ".tmp";
                module.Write(tempOutput);
                module.Dispose();
                
                File.Move(tempOutput, dllPath, true);
                Console.WriteLine("Success: Assembly patched successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex}");
                return 1;
            }
        }

        static int GetEnumValue(TypeDef enumType, string valueName)
        {
            var field = enumType.Fields.FirstOrDefault(f => f.Name == valueName);
            if (field != null && field.Constant != null)
            {
                return Convert.ToInt32(field.Constant.Value);
            }
            throw new Exception($"Enum value '{valueName}' not found in '{enumType.FullName}'");
        }
    }
}
