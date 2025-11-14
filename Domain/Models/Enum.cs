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
    public enum ConsultationStatus
    {
        Pending, Accepted, Rejected
    }
    public enum ProductLevels
    {
        Level1,Level2,Level3, Level4
    }
	public enum TimeSlot
	{
		Morning,    
		Afternoon,  
		Evening     
	}
	public enum SMSTemplateType
	{
		Registration,
	}
    public enum OrderStatus
    {
        OrderPlaced,
        Packed,
        Shipped,
        OutForDelivery,
        Delivered,
        Cancelled
    }
}
