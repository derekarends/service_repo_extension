using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using ThinkovatorInc.AddClassTemplate.Templates;

namespace ThinkovatorInc.AddClassTemplate
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidAddClassTemplatePkgString)]
    public sealed class AddClassTemplatePackage : Package
    {
        private readonly List<string> _supportedProjectExtensions = new List<string>
        {
            ".csproj",
            ".vbproj",
            ".fsproj"
        };

        protected override void Initialize()
        {
            base.Initialize();

            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                var menuCommandId = new CommandID(GuidList.guidAddClassTemplateCmdSet, (int)PkgCmdIDList.cmdidAddClassTemplate);
                var menuCommand = new OleMenuCommand(MenuItemCallback, menuCommandId);
                menuCommand.BeforeQueryStatus += OnBeforeQueryStatusAddTransformCommand;

                mcs.AddCommand(menuCommand);
            }
        }

        private static void OnBeforeQueryStatusAddTransformCommand(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            IVsHierarchy hierarchy;
            uint itemid;

            if (!IsSingleProjectSelected(out hierarchy, out itemid))
                return;

            menuCommand.Visible = true;
            menuCommand.Enabled = true;
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            IVsHierarchy hierarchy;
            uint itemid;

            if (!IsSingleProjectSelected(out hierarchy, out itemid))
                return;

            var vsProject = (IVsProject)hierarchy;
            if (!ProjectSupportsTransforms(vsProject))
                return;

            string projectFullPath;
            if (ErrorHandler.Failed(vsProject.GetMkDocument(VSConstants.VSITEMID_ROOT, out projectFullPath)))
                return;

            var buildPropertyStorage = vsProject as IVsBuildPropertyStorage;
            if (buildPropertyStorage == null)
                return;

            var solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
            var hr = solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, hierarchy, 0);
            if (hr < 0)
            {
                throw new COMException(string.Format("Failed to add project item"), hr);
            }

            var entry = new ClassEntry();
            entry.ShowDialog();
            if (string.IsNullOrEmpty(entry.BaseClassName))
                return;

            CreateFiles(entry, Path.GetDirectoryName(projectFullPath), hierarchy);
        }

        private static bool IsSingleProjectSelected(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
                return false;

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                IVsMultiItemSelect multiItemSelect;
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect, out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                    return false;

                if (multiItemSelect != null)
                    return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                    return false;

                var guidProjectId = Guid.Empty;
                if (ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectId)))
                    return false;

                return itemid == VSConstants.VSITEMID_ROOT;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                    Marshal.Release(selectionContainerPtr);

                if (hierarchyPtr != IntPtr.Zero)
                    Marshal.Release(hierarchyPtr);
            }
        }

        private bool ProjectSupportsTransforms(IVsProject project)
        {
            string projectFullPath;
            if (ErrorHandler.Failed(project.GetMkDocument(VSConstants.VSITEMID_ROOT, out projectFullPath)))
                return false;

            var projectExtension = Path.GetExtension(projectFullPath);

            foreach (var supportedExtension in _supportedProjectExtensions)
            {
                if (projectExtension.Equals(supportedExtension, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void CreateFiles(ClassEntry entry, string inputPath, IVsHierarchy hierarchy)
        {
            var directory = new DirectoryInfo(inputPath);
            var projectName = directory.Name;
            var path = Path.Combine(inputPath, Guid.NewGuid().ToString());

            if (entry.CreateRequestModel)
            {
                var requestTemplate = new RequestModelTemplate()
                {
                    Session = new Dictionary<string, object>
                    {
                        { "BaseClassName", entry.BaseClassName },
                        { "ProjectName", projectName }
                    }
                };
                requestTemplate.Initialize();

                var requestName = string.Format("{0}RequestModel.cs", entry.BaseClassName);
                var requestContent = requestTemplate.TransformText();

                AddFileToSolution(requestName, path, requestContent, hierarchy);
            }

            if (entry.CreateResponseModel)
            {
                var responseTemplate = new ResponseModelTemplate()
                {
                    Session = new Dictionary<string, object> 
                    { 
                        { "BaseClassName", entry.BaseClassName },
                        { "ProjectName", projectName } 
                    }
                };
                responseTemplate.Initialize();

                var responseName = string.Format("{0}ResponseModel.cs", entry.BaseClassName);
                var responseContent = responseTemplate.TransformText();

                AddFileToSolution(responseName, path, responseContent, hierarchy);
            }

            if (entry.CreateRepo)
            {
                var iRepoTemplate = new IRepoTemplate
                {
                    Session = new Dictionary<string, object>
                    {
                        { "BaseClassName", entry.BaseClassName },
                        { "ProjectName", projectName }
                    }
                };
                iRepoTemplate.Initialize();

                var iRepoName = string.Format("I{0}Repository.cs", entry.BaseClassName);
                var iRepoContent = iRepoTemplate.TransformText();

                AddFileToSolution(iRepoName, path, iRepoContent, hierarchy);


                var repoTemplate = new RepoTemplate
                {
                    Session = new Dictionary<string, object>
                    {
                        { "BaseClassName", entry.BaseClassName },
                        { "ProjectName", projectName }
                    }
                };
                repoTemplate.Initialize();

                var repoName = string.Format("{0}Repository.cs", entry.BaseClassName);
                var repoContent = repoTemplate.TransformText();

                AddFileToSolution(repoName, path, repoContent, hierarchy);
            }

            if (entry.CreateService)
            {
                var iServiceTemplate = new IServiceTemplate
                {
                    Session = new Dictionary<string, object>
                    {
                        { "BaseClassName", entry.BaseClassName },
                        { "ProjectName", projectName }
                    }
                };
                iServiceTemplate.Initialize();

                var iServiceName = string.Format("I{0}Service.cs", entry.BaseClassName);
                var iServiceContent = iServiceTemplate.TransformText();

                AddFileToSolution(iServiceName, path, iServiceContent, hierarchy);

                var serviceTemplate = new ServiceTemplate();
                serviceTemplate.Session = new Dictionary<string, object>
                {
                    {"BaseClassName", entry.BaseClassName},
                    {"CreatedRepo", entry.CreateRepo},
                    { "ProjectName", projectName }
                };
                serviceTemplate.Initialize();
                var serviceName = string.Format("{0}Service.cs", entry.BaseClassName);
                var serviceContent = serviceTemplate.TransformText();

                AddFileToSolution(serviceName, path, serviceContent, hierarchy);
            }

            if(Directory.Exists(path))
                Directory.Delete(path, true);

            var completed = new Complete();
            completed.ShowDialog();
        }

        private static void AddFileToSolution(string itemName, string path, string content, IVsHierarchy hierarchy)
        {
            var itemPath = Path.Combine(path, itemName);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(itemPath, content);
            if (!File.Exists(itemPath))
                return;

            var vsProject = (IVsProject)hierarchy;

            var result = new VSADDRESULT[1];
            vsProject.AddItem(
                VSConstants.VSITEMID_ROOT,
                VSADDITEMOPERATION.VSADDITEMOP_OPENFILE,
                itemPath,
                1,
                new[] { itemPath },
                IntPtr.Zero,
                result);
        }
    }
}
