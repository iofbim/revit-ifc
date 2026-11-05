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

using Newtonsoft.Json;

namespace Revit.IFC.Common.Utility
{
   /// <summary>
   /// Actor information required by the Turkish "E Proje Yonetim Sistemi" (E-PYS), as
   /// specified by the EK-7 IDS (ek7.ids).
   /// </summary>
   /// <remarks>
   /// Revit cannot author IfcPerson.Identification/Roles, IfcOrganization.Identification/Roles
   /// or IfcActorRole at all, so these values are collected in the export setup UI and
   /// serialized into the export configuration as JSON, following the COBieCompanyInfo pattern.
   ///
   /// All comparisons and case conversions in this class are deliberately culture
   /// invariant: on a Turkish system "i".ToUpper() is the dotted capital "I" with a dot,
   /// which would corrupt the ASCII IfcActorRole enumeration values.
   /// </remarks>
   public class EPYSActorInfo
   {
      /// <summary>
      /// The IfcActorRole value used when a role is not one of the IFC enumerated roles.
      /// </summary>
      public const string UserDefinedRoleValue = "USERDEFINED";

      /// <summary>
      /// IfcPerson.Identification. For E-PYS this carries the "Oda Sicil Numarasi"
      /// (chamber registration number) of the person.
      /// </summary>
      public string PersonIdentification { get; set; }

      /// <summary>
      /// IfcPerson.FamilyName.
      /// </summary>
      public string PersonFamilyName { get; set; }

      /// <summary>
      /// IfcPerson.GivenName.
      /// </summary>
      public string PersonGivenName { get; set; }

      /// <summary>
      /// The IfcActorRole.Role of the person. Either an IFC enumerated role or USERDEFINED.
      /// </summary>
      public string PersonRole { get; set; }

      /// <summary>
      /// IfcActorRole.UserDefinedRole for the person. Required when PersonRole is USERDEFINED.
      /// </summary>
      public string PersonUserDefinedRole { get; set; }

      /// <summary>
      /// IfcActorRole.Description for the person. Required by EK-7 for every role.
      /// </summary>
      public string PersonRoleDescription { get; set; }

      /// <summary>
      /// IfcOrganization.Identification.
      /// </summary>
      public string OrganizationIdentification { get; set; }

      /// <summary>
      /// IfcOrganization.Name.
      /// </summary>
      public string OrganizationName { get; set; }

      /// <summary>
      /// IfcOrganization.Description.
      /// </summary>
      public string OrganizationDescription { get; set; }

      /// <summary>
      /// The IfcActorRole.Role of the organization. Either an IFC enumerated role or USERDEFINED.
      /// </summary>
      public string OrganizationRole { get; set; }

      /// <summary>
      /// IfcActorRole.UserDefinedRole for the organization. Required when
      /// OrganizationRole is USERDEFINED.
      /// </summary>
      public string OrganizationUserDefinedRole { get; set; }

      /// <summary>
      /// IfcActorRole.Description for the organization. Required by EK-7 for every role.
      /// </summary>
      public string OrganizationRoleDescription { get; set; }

      /// <summary>
      /// The street address of the organization, for IfcPostalAddress.
      /// EK-7 requires IfcOrganization.Addresses.
      /// </summary>
      public string StreetAddress { get; set; }

      /// <summary>
      /// The town of the organization, for IfcPostalAddress.
      /// </summary>
      public string City { get; set; }

      /// <summary>
      /// The region of the organization, for IfcPostalAddress.
      /// </summary>
      public string Region { get; set; }

      /// <summary>
      /// The postal code of the organization, for IfcPostalAddress.
      /// </summary>
      public string PostalCode { get; set; }

      /// <summary>
      /// The country of the organization, for IfcPostalAddress.
      /// </summary>
      public string Country { get; set; }

      /// <summary>
      /// The telephone number of the organization, for IfcTelecomAddress.
      /// </summary>
      public string Telephone { get; set; }

      /// <summary>
      /// The e-mail address of the organization, for IfcTelecomAddress.
      /// </summary>
      public string ElectronicMail { get; set; }

      /// <summary>
      /// IfcProject.Name. E-PYS requires this to match a strict project code pattern.
      /// </summary>
      /// <remarks>
      /// Revit derives IfcProject.Name from the Project Number, so this is only used when
      /// the Revit Project Information does not supply one.
      /// </remarks>
      public string ProjectName { get; set; }

      /// <summary>
      /// IfcProject.Description.
      /// </summary>
      public string ProjectDescription { get; set; }

      /// <summary>
      /// Pset_ProjectCommon.ProjectType.
      /// </summary>
      public string ProjectType { get; set; }

      /// <summary>
      /// IfcProject.LongName.
      /// </summary>
      public string ProjectLongName { get; set; }

      /// <summary>
      /// IfcProject.Phase.
      /// </summary>
      public string ProjectPhase { get; set; }

      /// <summary>
      /// IfcProjectedCRS.VerticalDatum.
      /// </summary>
      /// <remarks>
      /// This and the three that follow have no field on the export GeoReference tab, so
      /// without them they can only be set through Revit project parameters. A blank value
      /// leaves the exporter's existing behaviour untouched.
      /// </remarks>
      public string CRSVerticalDatum { get; set; }

      /// <summary>
      /// IfcProjectedCRS.MapProjection.
      /// </summary>
      public string CRSMapProjection { get; set; }

      /// <summary>
      /// IfcProjectedCRS.MapZone.
      /// </summary>
      public string CRSMapZone { get; set; }

      /// <summary>
      /// IfcMapConversion.Scale, as typed on the tab.
      /// </summary>
      /// <remarks>
      /// Kept as a string so that "not set" stays distinct from zero: the exporter only
      /// applies a scale greater than zero, and falls back to its unit-ratio calculation
      /// otherwise. Use <see cref="GetMapConversionScale"/> to read it as a number.
      /// </remarks>
      public string MapConversionScale { get; set; }

      /// <summary>
      /// Whether the E-PYS actor information should be used to populate the exported
      /// IfcPerson, IfcOrganization and IfcPersonAndOrganization.
      /// </summary>
      public bool UseEPYSActorInfo { get; set; } = false;

      /// <summary>
      /// Whether the export should be blocked when the E-PYS requirements are not met.
      /// </summary>
      public bool ValidateOnExport { get; set; } = true;

      /// <summary>
      /// Whether the E-PYS information is also written to the IfcOrganization that
      /// IfcApplication refers to as its ApplicationDeveloper.
      /// </summary>
      /// <remarks>
      /// That organization identifies the authoring software vendor (Autodesk), not a
      /// participant in the project, so it is left alone by default. Enable this only when
      /// a checker requires every IfcOrganization in the file to carry the E-PYS
      /// attributes.
      /// </remarks>
      public bool ApplyToApplicationDeveloper { get; set; } = false;

      public EPYSActorInfo()
      {
      }

      public EPYSActorInfo(string actorInfoStr)
      {
         if (string.IsNullOrEmpty(actorInfoStr))
            return;

         EPYSActorInfo actorInfo = JsonConvert.DeserializeObject<EPYSActorInfo>(actorInfoStr);
         if (actorInfo == null)
            return;

         PersonIdentification = actorInfo.PersonIdentification;
         PersonFamilyName = actorInfo.PersonFamilyName;
         PersonGivenName = actorInfo.PersonGivenName;
         PersonRole = actorInfo.PersonRole;
         PersonUserDefinedRole = actorInfo.PersonUserDefinedRole;
         PersonRoleDescription = actorInfo.PersonRoleDescription;

         OrganizationIdentification = actorInfo.OrganizationIdentification;
         OrganizationName = actorInfo.OrganizationName;
         OrganizationDescription = actorInfo.OrganizationDescription;
         OrganizationRole = actorInfo.OrganizationRole;
         OrganizationUserDefinedRole = actorInfo.OrganizationUserDefinedRole;
         OrganizationRoleDescription = actorInfo.OrganizationRoleDescription;

         StreetAddress = actorInfo.StreetAddress;
         City = actorInfo.City;
         Region = actorInfo.Region;
         PostalCode = actorInfo.PostalCode;
         Country = actorInfo.Country;
         Telephone = actorInfo.Telephone;
         ElectronicMail = actorInfo.ElectronicMail;

         ProjectName = actorInfo.ProjectName;
         ProjectDescription = actorInfo.ProjectDescription;
         ProjectType = actorInfo.ProjectType;
         ProjectLongName = actorInfo.ProjectLongName;
         ProjectPhase = actorInfo.ProjectPhase;

         UseEPYSActorInfo = actorInfo.UseEPYSActorInfo;
         ValidateOnExport = actorInfo.ValidateOnExport;
         ApplyToApplicationDeveloper = actorInfo.ApplyToApplicationDeveloper;
      }

      /// <summary>
      /// Resolves a value, preferring the one entered on the E-PYS tab and falling back to
      /// the value authored in the Revit Project Information.
      /// </summary>
      /// <param name="revitValue">The value from the Revit Project Information.</param>
      /// <param name="epysValue">The value entered on the E-PYS tab.</param>
      /// <returns>The value to export.</returns>
      /// <remarks>
      /// The E-PYS tab is the authoritative source: what the user sees on the tab is what
      /// is exported. Revit only supplies the values the tab leaves blank, so that an
      /// existing model still exports its own project information when E-PYS adds nothing.
      /// </remarks>
      public static string ResolveProjectValue(string revitValue, string epysValue)
      {
         return !string.IsNullOrWhiteSpace(epysValue) ? epysValue.Trim() : NullIfBlank(revitValue);
      }

      static string NullIfBlank(string value)
      {
         return string.IsNullOrWhiteSpace(value) ? null : value;
      }

      /// <summary>
      /// The IfcMapConversion.Scale entered on the tab, as a number.
      /// </summary>
      /// <returns>
      /// The scale, or null when the field is blank or does not parse. Null means "not
      /// set", so the exporter keeps its own unit-ratio calculation.
      /// </returns>
      /// <remarks>
      /// Parsed with the invariant culture: on a Turkish system the decimal separator is a
      /// comma, so a value typed as "1.000002" would otherwise parse as 1000002.
      /// </remarks>
      public double? GetMapConversionScale()
      {
         if (string.IsNullOrWhiteSpace(MapConversionScale))
            return null;

         return double.TryParse(MapConversionScale.Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double value)
            ? value : (double?)null;
      }

      /// <summary>
      /// Whether a role value denotes the IFC USERDEFINED role.
      /// </summary>
      /// <param name="role">The role value.</param>
      /// <returns>True if the role is USERDEFINED.</returns>
      public static bool IsUserDefinedRole(string role)
      {
         // Ordinal: the IFC enumeration values are ASCII and must not be folded with the
         // Turkish casing rules.
         return string.Equals(role, UserDefinedRoleValue, System.StringComparison.OrdinalIgnoreCase);
      }

      /// <summary>
      /// Whether the organization has any address information to export.
      /// </summary>
      public bool HasAddressInfo()
      {
         return !string.IsNullOrWhiteSpace(StreetAddress)
            || !string.IsNullOrWhiteSpace(City)
            || !string.IsNullOrWhiteSpace(Region)
            || !string.IsNullOrWhiteSpace(PostalCode)
            || !string.IsNullOrWhiteSpace(Country)
            || !string.IsNullOrWhiteSpace(Telephone)
            || !string.IsNullOrWhiteSpace(ElectronicMail);
      }

      /// <summary>
      /// Whether the organization has postal address information to export.
      /// </summary>
      public bool HasPostalAddressInfo()
      {
         return !string.IsNullOrWhiteSpace(StreetAddress)
            || !string.IsNullOrWhiteSpace(City)
            || !string.IsNullOrWhiteSpace(Region)
            || !string.IsNullOrWhiteSpace(PostalCode)
            || !string.IsNullOrWhiteSpace(Country);
      }

      /// <summary>
      /// Whether the organization has telecom address information to export.
      /// </summary>
      public bool HasTelecomAddressInfo()
      {
         return !string.IsNullOrWhiteSpace(Telephone)
            || !string.IsNullOrWhiteSpace(ElectronicMail);
      }

      /// <summary>
      /// Validates this actor information against the EK-7 requirements.
      /// </summary>
      /// <returns>
      /// The list of unmet requirements, as resource-independent English identifiers of the
      /// missing IFC attributes. An empty list means the information is EK-7 compliant.
      /// </returns>
      public IList<string> Validate()
      {
         List<string> errors = new List<string>();

         // IfcPerson: Identification, FamilyName, GivenName and Roles are all required.
         if (string.IsNullOrWhiteSpace(PersonIdentification))
            errors.Add("IfcPerson.Identification");
         if (string.IsNullOrWhiteSpace(PersonFamilyName))
            errors.Add("IfcPerson.FamilyName");
         if (string.IsNullOrWhiteSpace(PersonGivenName))
            errors.Add("IfcPerson.GivenName");
         if (string.IsNullOrWhiteSpace(PersonRole))
            errors.Add("IfcPerson.Roles");

         // IfcActorRole: Description is required for every role, and UserDefinedRole is
         // additionally required when the role is USERDEFINED.
         if (string.IsNullOrWhiteSpace(PersonRoleDescription))
            errors.Add("IfcActorRole.Description (IfcPerson)");
         if (IsUserDefinedRole(PersonRole) && string.IsNullOrWhiteSpace(PersonUserDefinedRole))
            errors.Add("IfcActorRole.UserDefinedRole (IfcPerson)");

         // IfcOrganization: Identification, Name, Addresses and Roles are all required.
         if (string.IsNullOrWhiteSpace(OrganizationIdentification))
            errors.Add("IfcOrganization.Identification");
         if (string.IsNullOrWhiteSpace(OrganizationName))
            errors.Add("IfcOrganization.Name");
         if (!HasAddressInfo())
            errors.Add("IfcOrganization.Addresses");
         if (string.IsNullOrWhiteSpace(OrganizationRole))
            errors.Add("IfcOrganization.Roles");

         if (string.IsNullOrWhiteSpace(OrganizationRoleDescription))
            errors.Add("IfcActorRole.Description (IfcOrganization)");
         if (IsUserDefinedRole(OrganizationRole) && string.IsNullOrWhiteSpace(OrganizationUserDefinedRole))
            errors.Add("IfcActorRole.UserDefinedRole (IfcOrganization)");

         return errors;
      }

      /// <summary>
      /// Whether this actor information meets all of the EK-7 requirements.
      /// </summary>
      public bool IsValid()
      {
         return Validate().Count == 0;
      }

      public bool ElectronicMailValidator()
      {
         if (string.IsNullOrWhiteSpace(ElectronicMail))
            return true;

         Regex emailRegex = new Regex("^([A-Z0-9a-z_(-)(.)]+)@([A-Z0-9a-z_(-)]+)(.[A-Z0-9a-z_(-)]+)+$");
         return emailRegex.IsMatch(ElectronicMail);
      }

      public bool TelephoneValidator()
      {
         if (string.IsNullOrWhiteSpace(Telephone))
            return true;

         Regex phoneRegex = new Regex("(?<IDDCode>[+][0-9]{1,3}[ ]*)?(?<AreaCode>[(][0-9]{1,4}[)])?[ ]*(?<PhoneNumber>[0-9]{3,5}(?:-|[ ]*)[0-9]{3,4})");
         return phoneRegex.IsMatch(Telephone);
      }

      public string ToJsonString()
      {
         return JsonConvert.SerializeObject(this);
      }
   }
}
