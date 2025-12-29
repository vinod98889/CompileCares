// In Shared/Enums/DoctorType.cs
using System.ComponentModel;

namespace CompileCares.Shared.Enums
{
    public enum DoctorType
    {
        [Description("Consultant")]
        Consultant = 1,

        [Description("Senior Consultant")]
        SeniorConsultant = 2,  // This matches database value

        [Description("General Practitioner")]
        GeneralPractitioner = 3,

        [Description("Specialist")]
        Specialist = 4,

        [Description("Junior Consultant")]
        JuniorConsultant = 5
    }
}