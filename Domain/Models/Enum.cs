using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public enum UserRole
    {
        Admin, Employee, Doctor, Customer, 
    }
    public enum Specialities
    {
        HormoneOptimization, PerformanceEnhancement, StressManagement
    }
    public enum ConsultationType
    {
        VideoCall,VoiceCall,InPerson
    }
    public enum ProductLevels
    {
        Level1,Level2,Level3, Level4
    }
	public enum TimeSlot
	{
		Morning,    // 8:00 - 12:00
		Afternoon,  // 12:00 - 17:00
		Evening     // Optional, can add if needed
	}
	public enum SMSTemplateType
	{
		Registration,
	}
}
