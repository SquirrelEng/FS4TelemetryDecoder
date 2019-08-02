﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FS4TelemetryDecoder.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public double ReferenceLat {
            get {
                return ((double)(this["ReferenceLat"]));
            }
            set {
                this["ReferenceLat"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public double ReferenceLon {
            get {
                return ((double)(this["ReferenceLon"]));
            }
            set {
                this["ReferenceLon"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("-9999")]
        public double ReferenceAltM {
            get {
                return ((double)(this["ReferenceAltM"]));
            }
            set {
                this["ReferenceAltM"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.DateTime FlightTimeStart {
            get {
                return ((global::System.DateTime)(this["FlightTimeStart"]));
            }
            set {
                this["FlightTimeStart"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KJ6FO FS4 HAB <-- text from the WSJT LogFiles")]
        public string Type1Message {
            get {
                return ((string)(this["Type1Message"]));
            }
            set {
                this["Type1Message"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KJ6FO-11 APRS <-- text from the WSJT LogFiles")]
        public string Type4Message {
            get {
                return ((string)(this["Type4Message"]));
            }
            set {
                this["Type4Message"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Flying Squirrel 4 HAB <-- Message tht will display in APRS (Mission Name)")]
        public string APRS_FlightMessage {
            get {
                return ((string)(this["APRS_FlightMessage"]));
            }
            set {
                this["APRS_FlightMessage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/")]
        public string APRS_FlightSymbolTable {
            get {
                return ((string)(this["APRS_FlightSymbolTable"]));
            }
            set {
                this["APRS_FlightSymbolTable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("O")]
        public string APRS_FlightSymbol {
            get {
                return ((string)(this["APRS_FlightSymbol"]));
            }
            set {
                this["APRS_FlightSymbol"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MyCall-11")]
        public string APRS_FlightCallsignSSID {
            get {
                return ((string)(this["APRS_FlightCallsignSSID"]));
            }
            set {
                this["APRS_FlightCallsignSSID"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Ground Station.")]
        public string APRS_GroundMessage {
            get {
                return ((string)(this["APRS_GroundMessage"]));
            }
            set {
                this["APRS_GroundMessage"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("/")]
        public string APRS_GroundSymbolTable {
            get {
                return ((string)(this["APRS_GroundSymbolTable"]));
            }
            set {
                this["APRS_GroundSymbolTable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("k")]
        public string APRS_GroundSymbol {
            get {
                return ((string)(this["APRS_GroundSymbol"]));
            }
            set {
                this["APRS_GroundSymbol"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MyCall-10")]
        public string APRS_GroundCallsignSSID {
            get {
                return ((string)(this["APRS_GroundCallsignSSID"]));
            }
            set {
                this["APRS_GroundCallsignSSID"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("APRS PasswordHere")]
        public string APRS_Password {
            get {
                return ((string)(this["APRS_Password"]));
            }
            set {
                this["APRS_Password"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool APRS_Enable {
            get {
                return ((bool)(this["APRS_Enable"]));
            }
            set {
                this["APRS_Enable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("MyAPRSCallsign")]
        public string APRS_UserCallsign {
            get {
                return ((string)(this["APRS_UserCallsign"]));
            }
            set {
                this["APRS_UserCallsign"] = value;
            }
        }
    }
}
