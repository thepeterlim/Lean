﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Stores associations between a list of files and a QuantConnect project 
    /// that is associated with them.
    /// </summary>
    class ProjectFinder
    {
        private const string PROJECT_ASSOCIATIONS_FILE = "QuantConnectProjects.xml";
        private IDictionary<HashSet<string>, string> _projectForFiles 
            = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());

        public ProjectFinder()
        {
            ReadProjectAssociations();
        }

        private void ReadProjectAssociations()
        {
            try
            {
                var projectAssociations = ParseAssociationFile();
                FillProjectAssociations(projectAssociations);
            }
            catch (XmlException ex)
            {
                // Failed to read projects associations. Continuing with none.
            }
            catch (SerializationException ex)
            {
                // Failed to parse project associations.
            }
            catch (FileNotFoundException ex)
            {
                // Failed to parse project associations.
            }
        }

        private static ProjectAssociations ParseAssociationFile()
        {
            ProjectAssociations projectAssociations;
            using (var stream = new StreamReader(PROJECT_ASSOCIATIONS_FILE))
            using (var reader = new XmlTextReader(stream))
            {
                var dataContractSerializer = new DataContractSerializer(typeof(ProjectAssociations));
                projectAssociations = (ProjectAssociations)dataContractSerializer.ReadObject(reader);
            }

            return projectAssociations;
        }

        private void FillProjectAssociations(ProjectAssociations projectAssociations)
        {
            foreach (var projectAssociation in projectAssociations)
            {
                _projectForFiles.Add(new HashSet<string>(projectAssociation.FileNames), projectAssociation.ProjectName);
            }
        }

        /// <summary>
        /// Get a project name that is associated with the provided list of files
        /// </summary>
        /// <param name="files">List of files in a project</param>
        /// <returns>A name of a project if a list of files is associated with a project, empty string otherwise</returns>
        public string ProjectNameForFiles(List<string> files)
        {
            string projectName;
            if (_projectForFiles.TryGetValue(new HashSet<string>(files), out projectName))
            {
                return projectName;
            }
            return "";
        }

        /// <summary>
        /// Associate a project with a project name
        /// </summary>
        /// <param name="projectName">A project name to associate list of files with</param>
        /// <param name="files">A list of files to associate with a project name</param>
        public void AssociateProjectWith(string projectName, List<string> files)
        {
            SetProjectAssociation(projectName, files);
            UpdatAssociationsFile();
        }

        private void SetProjectAssociation(string projectName, List<string> files)
        {
            _projectForFiles[new HashSet<string>(files)] = projectName;
        }

        private void UpdatAssociationsFile()
        {
            var projectAssociations = CreateProjectAssociations();
            SerializeProjectAssociations(projectAssociations);
        }

        private static void SerializeProjectAssociations(ProjectAssociations projectAssociations)
        {
            using (var output = new StreamWriter(PROJECT_ASSOCIATIONS_FILE, false))
            using (var writer = new XmlTextWriter(output) { Formatting = Formatting.Indented })
            {
                var dataContractSerializer = new DataContractSerializer(typeof(ProjectAssociations));
                dataContractSerializer.WriteObject(writer, projectAssociations);
            }
        }

        private ProjectAssociations CreateProjectAssociations()
        {
            var projectAssociations = new ProjectAssociations();
            foreach (var a in _projectForFiles)
            {
                projectAssociations.Add(new ProjectAssociation(a.Value, new List<string>(a.Key)));
            }

            return projectAssociations;
        }
    }

    /// <summary>
    /// List of project associations
    /// </summary>
    [CollectionDataContract(Name = "ProjectAssociations")]
    class ProjectAssociations : List<ProjectAssociation>
    {
    }

    /// <summary>
    /// A pair that represent a project and list of files associated with it
    /// </summary>
    [DataContract(Name = "projectAssociation")]
    class ProjectAssociation
    {
        [DataMember(Name = "ProjectName")]
        private string _projectName;
        [DataMember(Name = "FileNames")]
        private List<string> _fileNames;

        public ProjectAssociation(string projectName, List<string> fileNames)
        {
            _projectName = projectName;
            _fileNames = fileNames;
        }

        public string ProjectName
        {
            get
            {
                return _projectName;
            }
        }

        public List<string> FileNames
        {
            get
            {
                return _fileNames;
            }
        }
    }
}
