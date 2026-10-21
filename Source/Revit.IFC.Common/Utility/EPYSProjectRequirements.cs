//
// Revit IFC Common library: this library works with Autodesk(R) Revit(R) to provide functionality shared by the IFC import and export libraries.
// Copyright (C) 2012 Autodesk, Inc.
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// The IfcProject requirements of the E-PYS EK-7 IDS (ek7.ids).
   /// </summary>
   /// <remarks>
   /// Unlike the actor information, IfcProject.Name, IfcProject.Description and
   /// Pset_ProjectCommon.ProjectType can all be authored in Revit through the Project
   /// Information. These values are therefore only validated, not collected.
   /// </remarks>
   public static class EPYSProjectRequirements
   {
      /// <summary>
      /// The EK-7 pattern for IfcProject.Name, taken verbatim from ek7.ids.
      /// </summary>
      /// <remarks>
      /// Written as eight digits or upper case letters, then groups of upper case letters
      /// of length 2, 4, 2 and 3, then a three character alphanumeric group, then a two
      /// and a three digit group; for example "12345678-AB-CDEF-GH-IJK-X1Y-01-123".
      ///
      /// The character classes are Unicode categories and plain ASCII ranges, so this
      /// pattern behaves identically under every culture, including tr-TR.
      /// </remarks>
      public const string ProjectNamePattern =
         @"[0-9\p{Lu}]{8}-\p{Lu}{2}-\p{Lu}{4}-\p{Lu}{2}-\p{Lu}{3}-[A-Z0-9]{3}-[0-9]{2}-[0-9]{3}";

      /// <summary>
      /// The property set that EK-7 requires IfcProject.ProjectType to be published in.
      /// </summary>
      public const string ProjectTypePropertySet = "Pset_ProjectCommon";

      /// <summary>
      /// The property that EK-7 requires in Pset_ProjectCommon.
      /// </summary>
      public const string ProjectTypeProperty = "ProjectType";

      // The IDS pattern facet matches the whole value, so anchor it: an unanchored match
      // would accept a valid code embedded in an otherwise invalid project name.
      static readonly Regex ProjectNameRegex =
         new Regex("^(?:" + ProjectNamePattern + ")$", RegexOptions.CultureInvariant);

      /// <summary>
      /// Whether a project name satisfies the EK-7 IfcProject.Name pattern.
      /// </summary>
      /// <param name="projectName">The IfcProject.Name value.</param>
      /// <returns>True if the name is EK-7 compliant.</returns>
      public static bool IsValidProjectName(string projectName)
      {
         return !string.IsNullOrEmpty(projectName) && ProjectNameRegex.IsMatch(projectName);
      }

      /// <summary>
      /// Validates the IfcProject values against the EK-7 requirements.
      /// </summary>
      /// <param name="projectName">The IfcProject.Name value.</param>
      /// <param name="projectDescription">The IfcProject.Description value.</param>
      /// <param name="projectType">The Pset_ProjectCommon.ProjectType value, if any.</param>
      /// <returns>
      /// The list of unmet requirements as IFC attribute identifiers. An empty list means
      /// the project information is EK-7 compliant.
      /// </returns>
      public static IList<string> Validate(string projectName, string projectDescription,
         string projectType)
      {
         List<string> errors = new List<string>();

         if (string.IsNullOrWhiteSpace(projectName))
            errors.Add("IfcProject.Name");
         else if (!IsValidProjectName(projectName))
            errors.Add("IfcProject.Name (" + ProjectNamePattern + ")");

         if (string.IsNullOrWhiteSpace(projectDescription))
            errors.Add("IfcProject.Description");

         if (string.IsNullOrWhiteSpace(projectType))
            errors.Add(ProjectTypePropertySet + "." + ProjectTypeProperty);

         return errors;
      }
   }
}
