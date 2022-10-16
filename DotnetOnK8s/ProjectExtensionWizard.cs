using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotnetOnK8s
{
    internal class ProjectExtensionWizard : IWizard
    {
        private Dictionary<string, string> _replacementsDictionary;
        //private string userName;
        //private string password;
        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public void ProjectFinishedGenerating(Project project)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            var dir = Path.GetDirectoryName(project.FileName);
            if (!Directory.Exists(Path.Combine(dir, "charts")))
                return;

            //copy docker and nuget.config files
            try
            {
                File.Move(Path.Combine(dir, "Dockerfile"), Path.Combine(Directory.GetParent(dir).FullName, "Dockerfile"));
                File.Move(Path.Combine(dir, ".gitignore"), Path.Combine(Directory.GetParent(dir).FullName, ".gitignore"));
            }
            catch (Exception)
            {
                MessageBox.Show("Error copying file 'Docker' and nuget.docker.config to project root folder. Please copy these manually");
            }
            ////replace the jfrog user and password
            //try

            //{
            //    if (File.Exists(Path.Combine(Directory.GetParent(dir).FullName, "nuget.docker.config")))
            //    {
            //        var text = File.ReadAllText(Path.Combine(Directory.GetParent(dir).FullName, "nuget.docker.config"));
            //        var replaced = text.Replace("jfrog_user", userName);
            //        replaced = replaced.Replace("jfrog_pass", password);
            //        File.Delete(Path.Combine(Directory.GetParent(dir).FullName, "nuget.docker.config"));
            //        File.WriteAllText(Path.Combine(Directory.GetParent(dir).FullName, "nuget.docker.config"), replaced);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Unable to replace ACR  userid and password. Please do this manually for the file nuget.docker.config");
            //}
            try
            {
                var di = Directory.CreateDirectory(Path.Combine(Directory.GetParent(dir).FullName, "charts"));
                di = Directory.CreateDirectory(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectname$"].ToLower().Replace("_", "-")}-service"));
                //move templates folder
                Directory.Move(Path.Combine(dir, Path.Combine("charts", "templates")), Path.Combine(di.FullName, "templates"));
                List<String> chartFiles = Directory.GetFiles(Path.Combine(dir, "charts"), "*.*").ToList();
                chartFiles.ForEach(o =>
                {
                    File.Move(o, Path.Combine(Path.Combine(Directory.GetParent(di.FullName).FullName, $"{_replacementsDictionary["$ext_safeprojectname$"].ToLower().Replace("_", "-")}-service"), new FileInfo(o).Name));
                });
                Directory.Delete(Path.Combine(dir, "charts"));
                //Directory.Move(Path.Combine(dir, "charts"), Path.Combine(Directory.GetParent(dir).FullName, Path.Combine(_replacementsDictionary["$ext_safeprojectname$"].ToLower(), "charts")));
                //Directory.Move(Path.Combine(dir, "charts"), Path.Combine(Directory.GetParent(dir).FullName, Path.Combine("charts", _replacementsDictionary["$ext_safeprojectname$"].ToLower())));
            }
            catch (Exception)
            {
                MessageBox.Show($"Unable to move charts folder under {dir} to {Directory.GetParent(dir).FullName}. Please move this folder manually");
            }

            //sql migrations
            if (!Directory.Exists(Path.Combine(dir, "migrations")))
                return;
            string migDateTime = $"{DateTime.Now.Year}{DateTime.Now.Month}{DateTime.Now.Day}{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}";
            try
            {
                var di = Directory.CreateDirectory(Path.Combine(Directory.GetParent(dir).FullName, @"migrations\sql"));
                File.Move(new FileInfo(Path.Combine(dir, $@"migrations\V1__{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}_Create.sql")).FullName, Path.Combine(Path.Combine(Directory.GetParent(di.FullName).FullName, @"sql"), $"V{migDateTime}__Create.sql"));
                Directory.Delete(Path.Combine(dir, "migrations"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to move sql file (V1__{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}_Create.sql) to {Directory.GetParent(dir).FullName}\\migrations\\sql folder. Please move this file manually");
            }
            try
            {
                File.WriteAllText(new FileInfo(Path.Combine(Directory.GetParent(dir).FullName, $@"migrations\{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-db-connection")).FullName, $"database:5432;username={_replacementsDictionary["$ext_safeprojectnamelowercase$"]};password={_replacementsDictionary["$ext_safeprojectnamelowercase$"]};database={_replacementsDictionary["$ext_safeprojectnamelowercase$"]}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying migrations files {Environment.NewLine} {ex.ToString()}");
            }


            var sln = project.DTE.Solution;
            var p = project.DTE.Solution.Projects;
            foreach (var proj in p)
            {

                if (proj is EnvDTE.Project)
                {
                    var subProject = (EnvDTE.Project)proj;
                    try
                    {
                        switch (subProject.Name)
                        {
                            case "buildassets":
                                var pi = subProject.ProjectItems.AddFromFile(Path.Combine(Directory.GetParent(dir).FullName, "Dockerfile"));
                                break;
                            case "charts":
                                foreach (var chartSubProject in subProject.ProjectItems)
                                {
                                    if (chartSubProject is EnvDTE.ProjectItem)
                                    {
                                        var folderName = ((EnvDTE.ProjectItem)chartSubProject).Name;
                                        if (Convert.ToString(folderName).ToLower() == $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service")
                                        {                                            
                                            ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service"), "Chart.yaml"));
                                            ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service"), "values.yaml"));
                                            ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service"), "values_dev.yaml"));
                                            ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service"), "values_local.yaml"));

                                            //var s = Microsoft.VisualBasic.Information.TypeName(p1);
                                            foreach (var subChartFolder in ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems)
                                            {
                                                //var o = ((dynamic)subChartFolder).Object;
                                                var o = ((EnvDTE.ProjectItem)subChartFolder).Name;
                                                if (o != null)
                                                {
                                                    //folderName = ((dynamic)o).Name;
                                                    if (Convert.ToString(o).ToLower() == "templates")
                                                    {
                                                        var subTemplateProject = ((EnvDTE.Project)((EnvDTE.ProjectItem)subChartFolder).Object);
                                                        //subTemplateProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service\\templates"), "database-migrations-job.yaml"));
                                                        subTemplateProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service\\templates"), "deployment.yaml"));
                                                        //subTemplateProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service\\templates"), "ingress.yaml"));
                                                        //subTemplateProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service\\templates"), "local-cluster-secrets.yaml"));
                                                        subTemplateProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "charts"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-service\\templates"), "service.yaml"));
                                                    }
                                                }

                                            }
                                        }
                                    }
                                }
                                break;
                            case "migrations":
                                subProject.ProjectItems.AddFromFile(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "migrations"), $"{_replacementsDictionary["$ext_safeprojectnamelowercase$"]}-db-connection"));
                                foreach (var chartSubProject in subProject.ProjectItems)
                                {
                                    if (chartSubProject is EnvDTE.ProjectItem)
                                    {
                                        var folderName = ((EnvDTE.ProjectItem)chartSubProject).Name;
                                        if (Convert.ToString(folderName).ToLower() == "sql")
                                        {                                            
                                            ((EnvDTE.Project)((EnvDTE.ProjectItem)chartSubProject).Object).ProjectItems.AddFromFile(Path.Combine(Path.Combine(Path.Combine(Directory.GetParent(dir).FullName, "migrations"), "sql"), $"V{migDateTime}__Create.sql"));
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show($"Error copying files in folder {subProject.Name} {Environment.NewLine} {ex.ToString()}");
                    }
                }
            }
        }


        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {

        }

        public void RunFinished()
        {

        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            _replacementsDictionary = replacementsDictionary;
            var currentrootProjName = _replacementsDictionary["$ext_safeprojectname$"];
            replacementsDictionary.Add("$ext_safeprojectnamelowercase$", currentrootProjName.ToLower().Replace("_", "-").Replace(".", ""));

            ////get user input
            //var form = new UserInputForm();            
            //form.ShowDialog();

            //userName = form.UserName;
            //password = form.Password;            
            #region not used
            //DTE dte = (DTE)automationObject;            
            //if (dte.SelectedItems.Count == 0)
            //    return;
            ////throw new Exception("Unable to locate selected item in the solution explorer");
            //var project = dte.SelectedItems.Item(1).Project;

            ////Determine which kind of project node: 
            //if (project != null)
            //{
            //    if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            //    {
            //        return;
            //    }
            //    else
            //    {
            //        //project
            //        replacementsDictionary.Add("$projectname$", project.Name);
            //    }
            //}
            #endregion

        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}