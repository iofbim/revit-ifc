//
// BIM IFC export alternate UI library: this library works with Autodesk(R) Revit(R) to provide an alternate user interface for the export of IFC files from Revit.
// Copyright (C) 2016  Autodesk, Inc.
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

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using Revit.IFC.Common.Utility;

namespace BIM.IFC.Export.UI
{
   /// <summary>
   /// Interaction logic for EPYSActorInfoTab.xaml.
   /// </summary>
   /// <remarks>
   /// Collects the IfcPerson, IfcOrganization, IfcPersonAndOrganization and IfcActorRole
   /// information required by the Turkish "E Proje Yonetim Sistemi" (E-PYS) EK-7
   /// specification, which cannot be authored through the Revit user interface.
   /// </remarks>
   public partial class EPYSActorInfoTab : UserControl
   {
      /// <summary>
      /// The IfcActorRole enumeration values allowed by EK-7, followed by USERDEFINED.
      /// </summary>
      /// <remarks>
      /// Listed explicitly rather than reflected off the IFC enumeration so that the set
      /// matches ek7.ids exactly: EK-7 permits these 22 roles plus USERDEFINED, and omits
      /// NOTDEFINED. The values are ASCII and must be written to the IFC file verbatim.
      /// </remarks>
      static readonly string[] EPYSActorRoles =
      [
         "ARCHITECT",
         "BUILDINGOPERATOR",
         "BUILDINGOWNER",
         "CIVILENGINEER",
         "CLIENT",
         "COMMISSIONINGENGINEER",
         "CONSTRUCTIONMANAGER",
         "CONSULTANT",
         "CONTRACTOR",
         "COSTENGINEER",
         "ELECTRICALENGINEER",
         "ENGINEER",
         "FACILITIESMANAGER",
         "FIELDCONSTRUCTIONMANAGER",
         "MANUFACTURER",
         "MECHANICALENGINEER",
         "OWNER",
         "PROJECTMANAGER",
         "RESELLER",
         "STRUCTURALENGINEER",
         "SUBCONTRACTOR",
         "SUPPLIER",
         EPYSActorInfo.UserDefinedRoleValue
      ];

      /// <summary>
      /// The values of PEnum_ProjectType, used for Pset_ProjectCommon.ProjectType.
      /// </summary>
      /// <remarks>
      /// These are the IFC4x3 enumeration values; they are ASCII and are written to the
      /// file verbatim.
      /// </remarks>
      static readonly string[] ProjectTypes =
      [
         "MODIFICATION",
         "NEWBUILD",
         "OPERATIONMAINTENANCE",
         "RENOVATION",
         "REPAIR"
      ];

      EPYSActorInfo m_ActorInfo;

      /// <summary>
      /// The E-PYS actor information as a JSON string, for storage in the export configuration.
      /// </summary>
      public string ActorInfoStr
      {
         get
         {
            PopulateActorInfo();
            return m_ActorInfo.ToJsonString();
         }
      }

      /// <summary>
      /// The E-PYS actor information currently shown in the tab.
      /// </summary>
      public EPYSActorInfo ActorInfo
      {
         get
         {
            PopulateActorInfo();
            return m_ActorInfo;
         }
      }

      public EPYSActorInfoTab(string actorInfoStr)
      {
         InitializeComponent();

         try
         {
            m_ActorInfo = new EPYSActorInfo(actorInfoStr);
         }
         catch (Newtonsoft.Json.JsonException)
         {
            // A configuration written by a different version must not break the dialog.
            m_ActorInfo = new EPYSActorInfo();
         }

         InitializeLabels();
         InitializeRoleLists();
         LoadActorInfo();
         ApplyRevitProjectInformation();
         UpdateEnabledState();

         // Both NavigateUri values are set here rather than in XAML: they are Uri valued and
         // the resource strings cannot be bound to them through x:Static.
         SetNavigateUri(hyperlink_Support, Properties.Resources.EPYSSupportUrl);
         SetNavigateUri(hyperlink_Mail, "mailto:" + Properties.Resources.EPYSSupportMail);
      }

      /// <summary>
      /// Assigns a navigation target to a hyperlink, ignoring a malformed address.
      /// </summary>
      static void SetNavigateUri(System.Windows.Documents.Hyperlink hyperlink, string uri)
      {
         try
         {
            hyperlink.NavigateUri = new Uri(uri);
         }
         catch (UriFormatException)
         {
            // A bad address must not stop the tab from loading; the link simply does nothing.
         }
      }

      /// <summary>
      /// Opens a support link in the user's browser or mail client.
      /// </summary>
      void hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
      {
         try
         {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
            {
               UseShellExecute = true
            });
         }
         catch (Exception)
         {
            // No browser or mail client registered: silently do nothing rather than
            // interrupting the export setup with an error.
         }

         e.Handled = true;
      }

      /// <summary>
      /// Sets the field labels, marking the attributes that EK-7 requires.
      /// </summary>
      void InitializeLabels()
      {
         string marker = Properties.Resources.EPYSRequiredFieldMarker;

         // EK-7 requires IfcProject Name, Description and Pset_ProjectCommon.ProjectType.
         // Georeferencing: none of these are EK-7 mandatory attributes, so no marker.
         label_CRSVerticalDatum.Content = Properties.Resources.EPYSCRSVerticalDatum;
         label_CRSMapProjection.Content = Properties.Resources.EPYSCRSMapProjection;
         label_CRSMapZone.Content = Properties.Resources.EPYSCRSMapZone;
         label_MapConversionScale.Content = Properties.Resources.EPYSMapConversionScale;

         label_ProjectName.Content = Required(Properties.Resources.EPYSProjectName, marker);
         label_ProjectLongName.Content = Properties.Resources.EPYSProjectLongName;
         label_ProjectDescription.Content = Required(Properties.Resources.EPYSProjectDescription, marker);
         label_ProjectType.Content = Required(Properties.Resources.EPYSProjectType, marker);
         label_ProjectPhase.Content = Properties.Resources.EPYSProjectPhase;

         label_PersonIdentification.Content = Required(Properties.Resources.EPYSPersonIdentification, marker);
         label_PersonFamilyName.Content = Required(Properties.Resources.EPYSPersonFamilyName, marker);
         label_PersonGivenName.Content = Required(Properties.Resources.EPYSPersonGivenName, marker);
         label_PersonRole.Content = Required(Properties.Resources.EPYSPersonRole, marker);
         label_PersonUserDefinedRole.Content = Properties.Resources.EPYSPersonUserDefinedRole;
         label_PersonRoleDescription.Content = Required(Properties.Resources.EPYSPersonRoleDescription, marker);

         label_OrganizationIdentification.Content = Required(Properties.Resources.EPYSOrganizationIdentification, marker);
         label_OrganizationName.Content = Required(Properties.Resources.EPYSOrganizationName, marker);
         label_OrganizationDescription.Content = Properties.Resources.EPYSOrganizationDescription;
         label_OrganizationRole.Content = Required(Properties.Resources.EPYSOrganizationRole, marker);
         label_OrganizationUserDefinedRole.Content = Properties.Resources.EPYSOrganizationUserDefinedRole;
         label_OrganizationRoleDescription.Content = Required(Properties.Resources.EPYSOrganizationRoleDescription, marker);

         // EK-7 requires IfcOrganization.Addresses, but not any particular address
         // attribute, so no single address field is marked as required.
         label_StreetAddress.Content = Properties.Resources.EPYSStreetAddress;
         label_City.Content = Properties.Resources.EPYSCity;
         label_Region.Content = Properties.Resources.EPYSRegion;
         label_PostalCode.Content = Properties.Resources.EPYSPostalCode;
         label_Country.Content = Properties.Resources.EPYSCountry;
         label_Telephone.Content = Properties.Resources.EPYSTelephone;
         label_ElectronicMail.Content = Properties.Resources.EPYSElectronicMail;
      }

      static string Required(string label, string marker)
      {
         return label + " " + marker;
      }

      void InitializeRoleLists()
      {
         // The combo boxes are optional until the user picks a role, so start with a blank
         // entry rather than silently defaulting to a role the user never chose.
         foreach (ComboBox roleComboBox in new[] { comboBox_PersonRole, comboBox_OrganizationRole })
         {
            roleComboBox.Items.Add(string.Empty);
            foreach (string role in EPYSActorRoles)
               roleComboBox.Items.Add(role);
         }

         comboBox_ProjectType.Items.Add(string.Empty);
         foreach (string projectType in ProjectTypes)
            comboBox_ProjectType.Items.Add(projectType);
      }

      /// <summary>
      /// Pre-fills the fields this tab leaves blank with the values already authored in the
      /// Revit Project Information, as an editable starting point. The tab wins on export,
      /// so nothing here is locked and a value the user typed is never overwritten.
      /// </summary>
      void ApplyRevitProjectInformation()
      {
         Autodesk.Revit.DB.ProjectInfo projectInfo = IFCCommandOverrideApplication.TheDocument?.ProjectInformation;
         if (projectInfo == null)
            return;

         // IfcProject.Name comes from the Project Number and IfcProject.LongName from the
         // Project Name, as per the IFC implementer's agreement.
         // Organization details Revit can already express, as an editable starting point.
         ShowRevitValue(textBox_OrganizationName, SafeGet(() => projectInfo.OrganizationName));
         ShowRevitValue(textBox_OrganizationDescription, SafeGet(() => projectInfo.OrganizationDescription));

         ShowRevitValue(textBox_ProjectName, SafeGet(() => projectInfo.Number));
         ShowRevitValue(textBox_ProjectLongName, SafeGet(() => projectInfo.Name));
         ShowRevitComboValue(comboBox_ProjectType, GetParameterValue(projectInfo, "ProjectType"));
         ShowRevitValue(textBox_ProjectPhase, GetParameterValue(projectInfo, "Project Phase"));
      }

      static string SafeGet(Func<string> getter)
      {
         try
         {
            return getter();
         }
         catch (Autodesk.Revit.Exceptions.ApplicationException)
         {
            return null;
         }
      }

      /// <summary>
      /// Reads a text parameter of the project information by name.
      /// </summary>
      static string GetParameterValue(Autodesk.Revit.DB.ProjectInfo projectInfo, string parameterName)
      {
         try
         {
            Autodesk.Revit.DB.Parameter parameter = projectInfo.LookupParameter(parameterName);
            if (parameter == null || !parameter.HasValue)
               return null;

            // Only read string parameters here. Parameter.AsValueString() formats the value
            // through the document and starts a transaction to do so, which is not allowed
            // from this dialog: it runs outside the Revit API context and Revit terminates
            // with "Starting a transaction from an external application running outside of
            // API context is not allowed".
            if (parameter.StorageType != Autodesk.Revit.DB.StorageType.String)
               return null;

            string value = parameter.AsString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
         }
         catch (Autodesk.Revit.Exceptions.ApplicationException)
         {
            // Reading project parameters must never bring down the export setup dialog.
            return null;
         }
      }

      /// <summary>
      /// Displays a value inherited from Revit in a combo box, and locks it.
      /// </summary>
      static void ShowRevitComboValue(ComboBox comboBox, string revitValue)
      {
         // Only pre-fill; the field stays editable because the tab wins over Revit.
         if (string.IsNullOrWhiteSpace(revitValue) || !string.IsNullOrWhiteSpace(GetSelectedRole(comboBox)))
            return;

         SelectRole(comboBox, revitValue);
         comboBox.ToolTip = Properties.Resources.EPYSProjectNote;
      }

      /// <summary>
      /// Displays a value inherited from Revit in a field, and locks the field.
      /// </summary>
      static void ShowRevitValue(TextBox textBox, string revitValue)
      {
         // The E-PYS tab wins over the Revit Project Information, so a value coming from
         // Revit is only an editable starting point: never overwrite what the user typed,
         // and never lock the field.
         if (string.IsNullOrWhiteSpace(revitValue) || !string.IsNullOrWhiteSpace(textBox.Text))
            return;

         textBox.Text = revitValue;
         textBox.ToolTip = Properties.Resources.EPYSProjectNote;
      }

      void LoadActorInfo()
      {
         checkBox_UseEPYSActorInfo.IsChecked = m_ActorInfo.UseEPYSActorInfo;
         checkBox_ValidateOnExport.IsChecked = m_ActorInfo.ValidateOnExport;
         checkBox_ApplyToApplicationDeveloper.IsChecked = m_ActorInfo.ApplyToApplicationDeveloper;

         textBox_CRSVerticalDatum.Text = m_ActorInfo.CRSVerticalDatum ?? string.Empty;
         textBox_CRSMapProjection.Text = m_ActorInfo.CRSMapProjection ?? string.Empty;
         textBox_CRSMapZone.Text = m_ActorInfo.CRSMapZone ?? string.Empty;
         textBox_MapConversionScale.Text = m_ActorInfo.MapConversionScale ?? string.Empty;

         textBox_ProjectName.Text = m_ActorInfo.ProjectName ?? string.Empty;
         textBox_ProjectLongName.Text = m_ActorInfo.ProjectLongName ?? string.Empty;
         textBox_ProjectDescription.Text = m_ActorInfo.ProjectDescription ?? string.Empty;
         SelectComboBoxValue(comboBox_ProjectType, m_ActorInfo.ProjectType);
         textBox_ProjectPhase.Text = m_ActorInfo.ProjectPhase ?? string.Empty;

         textBox_PersonIdentification.Text = m_ActorInfo.PersonIdentification ?? string.Empty;
         textBox_PersonFamilyName.Text = m_ActorInfo.PersonFamilyName ?? string.Empty;
         textBox_PersonGivenName.Text = m_ActorInfo.PersonGivenName ?? string.Empty;
         SelectRole(comboBox_PersonRole, m_ActorInfo.PersonRole);
         textBox_PersonUserDefinedRole.Text = m_ActorInfo.PersonUserDefinedRole ?? string.Empty;
         textBox_PersonRoleDescription.Text = m_ActorInfo.PersonRoleDescription ?? string.Empty;

         textBox_OrganizationIdentification.Text = m_ActorInfo.OrganizationIdentification ?? string.Empty;
         textBox_OrganizationName.Text = m_ActorInfo.OrganizationName ?? string.Empty;
         textBox_OrganizationDescription.Text = m_ActorInfo.OrganizationDescription ?? string.Empty;
         SelectRole(comboBox_OrganizationRole, m_ActorInfo.OrganizationRole);
         textBox_OrganizationUserDefinedRole.Text = m_ActorInfo.OrganizationUserDefinedRole ?? string.Empty;
         textBox_OrganizationRoleDescription.Text = m_ActorInfo.OrganizationRoleDescription ?? string.Empty;

         textBox_StreetAddress.Text = m_ActorInfo.StreetAddress ?? string.Empty;
         textBox_City.Text = m_ActorInfo.City ?? string.Empty;
         textBox_Region.Text = m_ActorInfo.Region ?? string.Empty;
         textBox_PostalCode.Text = m_ActorInfo.PostalCode ?? string.Empty;
         textBox_Country.Text = m_ActorInfo.Country ?? string.Empty;
         textBox_Telephone.Text = m_ActorInfo.Telephone ?? string.Empty;
         textBox_ElectronicMail.Text = m_ActorInfo.ElectronicMail ?? string.Empty;
      }

      /// <summary>
      /// Selects a role in a combo box.
      /// </summary>
      /// <remarks>
      /// The comparison is ordinal: the IFC role values are ASCII, and a Turkish culture
      /// comparison would not reliably match "I" against "i".
      /// </remarks>
      static void SelectComboBoxValue(ComboBox comboBox, string value)
      {
         SelectRole(comboBox, value);
      }

      static void SelectRole(ComboBox roleComboBox, string role)
      {
         if (string.IsNullOrWhiteSpace(role))
         {
            roleComboBox.SelectedIndex = 0;
            return;
         }

         string roleValue = role.Trim().ToUpperInvariant();
         foreach (object item in roleComboBox.Items)
         {
            if (string.Equals(item as string, roleValue, StringComparison.Ordinal))
            {
               roleComboBox.SelectedItem = item;
               return;
            }
         }

         // A role that is not in the EK-7 list is kept so that it is not silently lost.
         roleComboBox.Items.Add(roleValue);
         roleComboBox.SelectedItem = roleValue;
      }

      static string GetSelectedRole(ComboBox roleComboBox)
      {
         string role = roleComboBox.SelectedItem as string;
         return string.IsNullOrWhiteSpace(role) ? null : role;
      }

      void PopulateActorInfo()
      {
         m_ActorInfo.UseEPYSActorInfo = checkBox_UseEPYSActorInfo.IsChecked == true;
         m_ActorInfo.ValidateOnExport = checkBox_ValidateOnExport.IsChecked == true;
         m_ActorInfo.ApplyToApplicationDeveloper = checkBox_ApplyToApplicationDeveloper.IsChecked == true;

         m_ActorInfo.CRSVerticalDatum = Trimmed(textBox_CRSVerticalDatum.Text);
         m_ActorInfo.CRSMapProjection = Trimmed(textBox_CRSMapProjection.Text);
         m_ActorInfo.CRSMapZone = Trimmed(textBox_CRSMapZone.Text);
         m_ActorInfo.MapConversionScale = Trimmed(textBox_MapConversionScale.Text);

         m_ActorInfo.ProjectName = Trimmed(textBox_ProjectName.Text);
         m_ActorInfo.ProjectLongName = Trimmed(textBox_ProjectLongName.Text);
         m_ActorInfo.ProjectDescription = Trimmed(textBox_ProjectDescription.Text);
         m_ActorInfo.ProjectType = GetSelectedRole(comboBox_ProjectType);
         m_ActorInfo.ProjectPhase = Trimmed(textBox_ProjectPhase.Text);

         m_ActorInfo.PersonIdentification = Trimmed(textBox_PersonIdentification.Text);
         m_ActorInfo.PersonFamilyName = Trimmed(textBox_PersonFamilyName.Text);
         m_ActorInfo.PersonGivenName = Trimmed(textBox_PersonGivenName.Text);
         m_ActorInfo.PersonRole = GetSelectedRole(comboBox_PersonRole);
         m_ActorInfo.PersonUserDefinedRole = Trimmed(textBox_PersonUserDefinedRole.Text);
         m_ActorInfo.PersonRoleDescription = Trimmed(textBox_PersonRoleDescription.Text);

         m_ActorInfo.OrganizationIdentification = Trimmed(textBox_OrganizationIdentification.Text);
         m_ActorInfo.OrganizationName = Trimmed(textBox_OrganizationName.Text);
         m_ActorInfo.OrganizationDescription = Trimmed(textBox_OrganizationDescription.Text);
         m_ActorInfo.OrganizationRole = GetSelectedRole(comboBox_OrganizationRole);
         m_ActorInfo.OrganizationUserDefinedRole = Trimmed(textBox_OrganizationUserDefinedRole.Text);
         m_ActorInfo.OrganizationRoleDescription = Trimmed(textBox_OrganizationRoleDescription.Text);

         m_ActorInfo.StreetAddress = Trimmed(textBox_StreetAddress.Text);
         m_ActorInfo.City = Trimmed(textBox_City.Text);
         m_ActorInfo.Region = Trimmed(textBox_Region.Text);
         m_ActorInfo.PostalCode = Trimmed(textBox_PostalCode.Text);
         m_ActorInfo.Country = Trimmed(textBox_Country.Text);
         m_ActorInfo.Telephone = Trimmed(textBox_Telephone.Text);
         m_ActorInfo.ElectronicMail = Trimmed(textBox_ElectronicMail.Text);
      }

      static string Trimmed(string value)
      {
         return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
      }

      /// <summary>
      /// Enables the input fields only while the E-PYS actor information is in use, and the
      /// UserDefinedRole fields only for the USERDEFINED role.
      /// </summary>
      void UpdateEnabledState()
      {
         bool inUse = checkBox_UseEPYSActorInfo.IsChecked == true;

         checkBox_ValidateOnExport.IsEnabled = inUse;
         checkBox_ApplyToApplicationDeveloper.IsEnabled = inUse;
         grid_Project.IsEnabled = inUse;
         grid_Person.IsEnabled = inUse;
         grid_Organization.IsEnabled = inUse;
         grid_Address.IsEnabled = inUse;

         // IfcActorRole.UserDefinedRole only applies to the USERDEFINED role; EK-7 requires
         // IfcActorRole.Description for every role, so that field stays available.
         textBox_PersonUserDefinedRole.IsEnabled =
            inUse && EPYSActorInfo.IsUserDefinedRole(GetSelectedRole(comboBox_PersonRole));
         textBox_OrganizationUserDefinedRole.IsEnabled =
            inUse && EPYSActorInfo.IsUserDefinedRole(GetSelectedRole(comboBox_OrganizationRole));
      }

      void checkBox_UseEPYSActorInfo_Changed(object sender, RoutedEventArgs e)
      {
         UpdateEnabledState();
      }

      void comboBox_PersonRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         UpdateEnabledState();
      }

      void comboBox_OrganizationRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         UpdateEnabledState();
      }

      void textBox_MapConversionScale_LostFocus(object sender, RoutedEventArgs e)
      {
         m_ActorInfo.MapConversionScale = Trimmed(textBox_MapConversionScale.Text);

         // Blank is valid and means "not set": the exporter then keeps its own unit-ratio
         // calculation. Only a value that is present but unusable is flagged.
         double? scale = m_ActorInfo.GetMapConversionScale();
         bool isValid = string.IsNullOrWhiteSpace(m_ActorInfo.MapConversionScale)
            || (scale.HasValue && scale.Value > 0.0);

         ShowValidation(textBlock_MapConversionScaleValidation, isValid,
            Properties.Resources.EPYSInvalidScale);
      }

      void textBox_Telephone_LostFocus(object sender, RoutedEventArgs e)
      {
         m_ActorInfo.Telephone = Trimmed(textBox_Telephone.Text);
         ShowValidation(textBlock_TelephoneValidation, m_ActorInfo.TelephoneValidator(),
            Properties.Resources.EPYSInvalidTelephone);
      }

      void textBox_ElectronicMail_LostFocus(object sender, RoutedEventArgs e)
      {
         m_ActorInfo.ElectronicMail = Trimmed(textBox_ElectronicMail.Text);
         ShowValidation(textBlock_ElectronicMailValidation, m_ActorInfo.ElectronicMailValidator(),
            Properties.Resources.EPYSInvalidEmail);
      }

      static void ShowValidation(System.Windows.Controls.TextBlock indicator, bool isValid, string message)
      {
         if (isValid)
         {
            indicator.Visibility = Visibility.Hidden;
            indicator.ToolTip = null;
         }
         else
         {
            indicator.Text = "!";
            indicator.ToolTip = message;
            indicator.Visibility = Visibility.Visible;
         }
      }
   }
}
