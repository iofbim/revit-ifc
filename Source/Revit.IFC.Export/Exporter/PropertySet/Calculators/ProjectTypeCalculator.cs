//
// BIM IFC library: this library works with Autodesk(R) Revit(R) to export IFC files containing model geometry.
// Copyright (C) 2012  Autodesk, Inc.
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

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

using Revit.IFC.Common.Utility;
using Revit.IFC.Export.Utility;

namespace Revit.IFC.Export.Exporter.PropertySet.Calculators
{
   /// <summary>
   /// A calculation class to calculate Pset_ProjectCommon.ProjectType.
   /// </summary>
   /// <remarks>
   /// E-PYS requires Pset_ProjectCommon.ProjectType on IfcProject. Revit has no built in
   /// parameter for it, so without this calculator the property is only written when the
   /// user happens to have added a project parameter called "ProjectType", and the whole
   /// property set is omitted otherwise.
   ///
   /// Following the general rule for calculators, the value authored in Revit wins and the
   /// E-PYS export setup only supplies what Revit leaves blank.
   /// </remarks>
   class ProjectTypeCalculator : PropertyCalculator
   {
      /// <summary>
      /// The calculated value.
      /// </summary>
      private string m_ProjectType = string.Empty;

      static ProjectTypeCalculator s_Instance = new ProjectTypeCalculator();

      /// <summary>
      /// The ProjectTypeCalculator instance.
      /// </summary>
      public static ProjectTypeCalculator Instance
      {
         get { return s_Instance; }
      }

      /// <summary>
      /// Calculates the project type value.
      /// </summary>
      /// <param name="exporterIFC">The ExporterIFC object.</param>
      /// <param name="handle">The created IFC handle.</param>
      /// <param name="extrusionCreationData">The IFCExportBodyParams.</param>
      /// <param name="element">The element to calculate the value.</param>
      /// <param name="elementType">The element type.</param>
      /// <param name="entryMap">The corresponding property set entry map.</param>
      /// <returns>True if a value was found, false otherwise.</returns>
      public override bool Calculate(ExporterIFC exporterIFC, IFCAnyHandle handle, IFCExportBodyParams extrusionCreationData,
         Element element, ElementType elementType, EntryMap entryMap)
      {
         // A value authored in Revit always takes precedence.
         (_, m_ProjectType) = ParameterUtil.GetStringValueFromElementOrSymbol(element, null, false,
            entryMap.RevitParameterName, entryMap.CompatibleRevitParameterName);
         if (!string.IsNullOrWhiteSpace(m_ProjectType))
            return true;

         // Otherwise fall back to the value entered on the E-PYS tab.
         EPYSActorInfo epysActorInfo = ExporterCacheManager.ExportOptionsCache.EPYSActorInfo;
         m_ProjectType = epysActorInfo?.ProjectType;

         return !string.IsNullOrWhiteSpace(m_ProjectType);
      }

      /// <summary>
      /// Gets the calculated string value.
      /// </summary>
      /// <returns>The project type.</returns>
      public override string GetStringValue()
      {
         return m_ProjectType;
      }
   }
}
