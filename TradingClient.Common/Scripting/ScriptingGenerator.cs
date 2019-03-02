using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using TradingClient.Data.Contracts;
using TradingClient.Interfaces;


namespace TradingClient.Common
{
    public class ScriptingGenerator : IScriptingGenerator
    {
        private readonly IPathManager _folders;

        public ScriptingGenerator(IPathManager folderManager)
        {
            _folders = folderManager;
        }

        public string CreateIndicator(string path, IIndicatorSettings settings)
        {
            var template = File.ReadAllText(Path.Combine("Scripting templates", "Indicator.template"));

            template = SetDefaultProperty(template, settings);

            if (settings.Series.Count() > 0)
            {
                var seriesDescription = new StringBuilder();

                foreach (var series in settings.Series)
                {
                    seriesDescription.AppendFormat("{0}Series.Add(new Series(\"{1}\"));{2}", "\t\t\t", series.Name, Environment.NewLine);
                }

                template = template.Replace("//{@Generated series}", seriesDescription.ToString());
            }
            else
            {
                template = template.Replace("//{@Generated series}", "\t\t\t// Indicator series must be here");
            }

            if (settings.Parameters.Count > 0 || settings.Series.Count > 0)
            {
                var properties = new StringBuilder();
                var propertiesSetter = new StringBuilder();

                properties.AppendFormat("{1}return new List<ScriptingParameterBase>{0}", Environment.NewLine, "\t\t\t");
                properties.AppendLine("\t\t\t{");
                int i = 0;

                foreach (var series in settings.Series)
                {
                    properties.AppendFormat("{3}new SeriesParam(\"{0}\", \"Series parameters\", {1}),{2}", series.Name, i, Environment.NewLine, "\t\t\t\t");
                    i++;
                }

                var propertiesDeclaration = new StringBuilder();

                foreach (var property in settings.Parameters)
                {
                    propertiesDeclaration.AppendFormat("{5}public {0} {1} {3} get; set; {4}{2}",
                        GetObjectType(property.Type), property.Name,
                        Environment.NewLine, "{", "}", "\t\t");

                    properties.Append(GetPropertyEditor(property, i));

                    propertiesSetter.AppendFormat("{4}{0} = (({1})parameterBases[{2}]).Value;{3}", 
                        property.Name, GetEditorType(property), i, Environment.NewLine, "\t\t\t");
                    i++;
                }

                properties.Append("\t\t\t};");

                template = template.Replace("//{@Properties}", propertiesDeclaration.ToString());
                template = template.Replace("//{@InternalGetParameters}", properties.ToString());
                template = template.Replace("//{@InternalSetParameters}", propertiesSetter.ToString());
            }
            else if (settings.Parameters.Count == 0)
            {
                template = template.Replace("//{@Properties}", "");
                template = template.Replace("//{@InternalSetParameters}", "");
                template = template.Replace("//{@InternalGetParameters}", "\t\t\treturn new List<ScriptingParameterBase>();");
            }

            var destinationPath = Path.Combine(_folders.IndicatorsDirectory, settings.Name);
            try
            {
                DeploySolution(destinationPath, settings, template);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return string.Empty;
        }
        
        public string CreateSignal(string path, ISignalSettings settings)
        {
            var template = File.ReadAllText(Path.Combine("Scripting templates", "Signal.template"));

            template = SetDefaultProperty(template, settings);

            if (settings.Parameters.Count > 0)
            {
                const int templateParamOffset = 16;
                string newLine = Environment.NewLine;
                var properties = new StringBuilder();
                var propertiesSetter = new StringBuilder();
                var propertiesDeclaration = new StringBuilder();
                var backtestVars = new StringBuilder();
                var numericPropNames = new StringBuilder();

                for (int i = 0, j = 0; i < settings.Parameters.Count; i++)
                {
                    var p = settings.Parameters[i];
                    propertiesDeclaration.AppendFormat("{5}public {0} {1} {3} get; set; {4}{2}",
                        GetObjectType(p.Type), p.Name, newLine, "{", "}", "\t\t");

                    properties.Append(GetPropertyEditor(p, i+templateParamOffset));

                    propertiesSetter.AppendFormat("{4}{0} = (({1})parameterBases[{2}]).Value;{3}",
                        p.Name, GetEditorType(p), i+templateParamOffset, newLine, "\t\t\t");

                    if (p.Type == ScriptingParameterTypes.Double)
                    {
                        numericPropNames.Append(p.Name + ", ");
                        backtestVars.AppendFormat("\t\t\tdouble current_{0} = (double)parameterItem.ElementAt({1});{2}", p.Name, templateParamOffset+j++, newLine);
                    }
                    else if (p.Type == ScriptingParameterTypes.Int)
                    {
                        numericPropNames.Append(p.Name + ", ");
                        backtestVars.AppendFormat("\t\t\tint current_{0} = (int)parameterItem.ElementAt({1});{2}", p.Name, templateParamOffset+j++, newLine);
                    }
                    else if (p.Type == ScriptingParameterTypes.Bool)
                    {
                        numericPropNames.Append(p.Name + ", ");
                        backtestVars.AppendFormat("\t\t\tint current_{0} = (int)parameterItem.ElementAt({1});{2}", p.Name, templateParamOffset + j++, newLine);
                    }
                }
                //properties.Append("\t\t\t};");

                template = template.Replace("//{@Properties}", propertiesDeclaration.ToString());
                template = template.Replace("//{@InternalGetParameters}", properties.ToString());
                template = template.Replace("//{@InternalSetParameters}", propertiesSetter.ToString());
                template = template.Replace("//{@NumericParams}", numericPropNames.Length > 2 
                    ? numericPropNames.Remove(numericPropNames.Length - 2, 2).ToString() : numericPropNames.ToString());
            }
            else
            {
                template = template.Replace("//{@Properties}", "");
                template = template.Replace("//{@InternalSetParameters}", "");
                template = template.Replace("//{@InternalGetParameters}", "");
                template = template.Replace("//{@BacktestParamValues}", "");
                template = template.Replace("//{@NumericParams}", "");
            }

            var destinationPath = Path.Combine(_folders.SignalsDirectory, settings.Name);
            try
            {
                DeploySolution(destinationPath, settings, template);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return String.Empty;
        }

        #region Private methods

        private string GetPropertyEditor(IScriptingParameter property, int index)
        {
            var result = new StringBuilder();

            switch (property.Type)
            {
                case ScriptingParameterTypes.Int:
                case ScriptingParameterTypes.Double:

                    if (property.Type == ScriptingParameterTypes.Int)
                    {
                        result.AppendFormat("{4}new IntParam(\"{0}\", \"{1}\", {2}){3}",
                            property.Name, property.Description, index, Environment.NewLine, "\t\t\t\t");
                    }
                    else
                    {
                        result.AppendFormat("{4}new DoubleParam(\"{0}\", \"{1}\", {2}){3}",
                            property.Name, property.Description, index, Environment.NewLine, "\t\t\t\t");
                    }

                    result.AppendLine("\t\t\t\t{");
                    result.AppendFormat("{2}Value = {0},{1}", property.Value, Environment.NewLine, "\t\t\t\t\t");
                    result.AppendLine("\t\t\t\t},");
                    break;
                case ScriptingParameterTypes.String:
                    result.AppendFormat("{4}new StringParam(\"{0}\", \"{1}\", {2}){3}",
                        property.Name, property.Description, index, Environment.NewLine, "\t\t\t\t");
                    result.AppendLine("\t\t\t\t{");
                    result.AppendFormat("{2}Value = \"{0}\",{1}", property.Value, Environment.NewLine, "\t\t\t\t\t");
                    result.AppendLine("\t\t\t\t},");
                    break;
                case ScriptingParameterTypes.Bool:
                    result.AppendFormat("{4}new BoolParam(\"{0}\", \"{1}\", {2}){3}",
                        property.Name, property.Description, index, Environment.NewLine, "\t\t\t\t");
                    result.AppendLine("\t\t\t\t{");
                    result.AppendFormat("{2}Value = {0},{1}", property.Value.ToString().ToLower(), Environment.NewLine, "\t\t\t\t\t");
                    result.AppendLine("\t\t\t\t},");
                    break;
            }

            return result.ToString();
        }

        private string GetEditorType(IScriptingParameter property)
        {
            switch (property.Type)
            {
                case ScriptingParameterTypes.Int:
                    return nameof(IntParam);
                case ScriptingParameterTypes.Double:
                    return nameof(DoubleParam);
                case ScriptingParameterTypes.String:
                    return nameof(StringParam);
                case ScriptingParameterTypes.Bool:
                    return nameof(BoolParam);
                default:
                    throw new InvalidDataException("Invalid data type exception.");
            }
        }
        
        private string SetDefaultProperty(string template, IScriptingSettings settings)
        {
            //var author = settings.DefaultProperties.FirstOrDefault(p => p.Name.Equals(DefaultPropertyNames.Author));
            //var link = settings.DefaultProperties.FirstOrDefault(p => p.Name.Equals(DefaultPropertyNames.Link));
            //var separateWindow = settings.DefaultProperties.FirstOrDefault(p => p.Name.Equals(DefaultPropertyNames.InSeparateWindow));

            string description = "	/// " + settings.Name + (settings is SignalSettings ? " signal" : " indicator");
            if (!String.IsNullOrEmpty(settings.Description))
            {
                description = "	/// <summary>";
                description += ("\r\n	/// " + settings.Description);
                if (!String.IsNullOrEmpty(settings.Author))
                    description += ("\r\n	/// " + settings.Author);
                if (!String.IsNullOrEmpty(settings.Link))
                    description += ("\r\n	/// " + settings.Link);
                description += "\r\n	/// </summary>";
            }
            template = template.Replace("//{@Description}", description);

            bool separatePanel = false;

            template = template.Replace("//{@IsOverlay}", (!separatePanel).ToString().ToLowerInvariant());
            template = template.Replace("//{@Name}", settings.Name);
            template = template.Replace("//{@namespace}", settings.Name);

            return template;
        }

        private void DeploySolution(string destinationPath, IScriptingSettings settings, string template)
        {
            Extentions.UnzipFile(_folders.DebugServicesFileName, destinationPath);

            // Create scripting instance file
            var ScriptingInstanceFile = Path.Combine(destinationPath, "ScriptingInstance", settings.Name + ".cs");
            File.WriteAllText(ScriptingInstanceFile, template);

            // Modify project file
            var cprojFileName = Path.Combine(destinationPath, "ScriptingInstance", "ScriptingInstance.csproj");
            var cproj = File.ReadAllText(cprojFileName);
            File.Delete(cprojFileName);
            var newCprojFileName = Path.Combine(destinationPath, "ScriptingInstance", settings.Name + ".csproj");
            File.WriteAllText(newCprojFileName, cproj.Replace("ScriptingInstance", settings.Name));

            // Modify AssemblyInfo file
            var infoFileName = Path.Combine(destinationPath, "ScriptingInstance", "Properties", "AssemblyInfo.cs");
            var info = File.ReadAllText(infoFileName);
            File.Delete(infoFileName);
            File.WriteAllText(infoFileName, info.Replace("ScriptingInstance", settings.Name));

            // Rename ScriptingInstance folder
            var oldName = Path.Combine(destinationPath, "ScriptingInstance");
            var newName = Path.Combine(destinationPath, settings.Name);
            if (!oldName.Equals(newName))
                Directory.Move(oldName, newName);

            // Modify Scripting solution file
            var solutionFileName = Path.Combine(destinationPath, "Scripting.sln");
            var newSolutionFileName = Path.Combine(destinationPath, settings.Name + ".sln");
            var solution = File.ReadAllText(solutionFileName);
            File.WriteAllText(newSolutionFileName, solution.Replace("ScriptingInstance", settings.Name));
            File.Delete(solutionFileName);
            //SetStartupProject(newSolutionFileName, settings.Name);  //optional

            // Modify Server.cs file
            var serverFileName = Path.Combine(destinationPath, "SimulatedServer", "Server.cs");
            var server = File.ReadAllText(serverFileName);
            File.WriteAllText(serverFileName, server.Replace("ScriptingInstance", settings.Name));

            // Modify Server.cs file
            var serverCprojPath = Path.Combine(destinationPath, "SimulatedServer", "SimulatedServer.csproj");
            var serverCproj = File.ReadAllText(serverCprojPath);
            serverCproj = serverCproj.Replace("ScriptingInstance", settings.Name);
            File.WriteAllText(serverCprojPath, serverCproj);

            Process.Start(newSolutionFileName);
        }

        private string GetObjectType(ScriptingParameterTypes type)
        {
            switch (type)
            {
                case ScriptingParameterTypes.Int:
                    return "int";
                case ScriptingParameterTypes.Double:
                    return "double";
                case ScriptingParameterTypes.Bool:
                    return "bool";
                case ScriptingParameterTypes.String:
                    return "string";
                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
