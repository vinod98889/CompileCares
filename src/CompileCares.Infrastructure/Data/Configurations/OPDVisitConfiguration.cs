using CompileCares.Core.Entities.Billing;
using CompileCares.Core.Entities.Clinical;
using CompileCares.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompileCares.Infrastructure.Data.Configurations
{
    public class OPDVisitConfiguration : BaseEntityConfiguration<OPDVisit>
    {
        public override void Configure(EntityTypeBuilder<OPDVisit> builder)
        {
            base.Configure(builder);
            builder.ToTable("OPDVisits");

            builder.HasKey(v => v.Id);

            // Visit Information
            builder.Property(v => v.VisitNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(v => v.VisitDate)
                .IsRequired();

            builder.Property(v => v.VisitType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(v => v.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Clinical Information
            builder.Property(v => v.ChiefComplaint)
                .HasMaxLength(500);

            builder.Property(v => v.HistoryOfPresentIllness)
                .HasMaxLength(2000);

            builder.Property(v => v.PastMedicalHistory)
                .HasMaxLength(1000);

            builder.Property(v => v.FamilyHistory)
                .HasMaxLength(1000);

            builder.Property(v => v.ClinicalNotes)
                .HasMaxLength(4000);

            builder.Property(v => v.Diagnosis)
                .HasMaxLength(1000);

            builder.Property(v => v.TreatmentPlan)
                .HasMaxLength(2000);

            // Vitals
            builder.Property(v => v.BloodPressure)
                .HasMaxLength(20);

            builder.Property(v => v.Temperature)
                .HasColumnType("decimal(5,2)");

            builder.Property(v => v.Pulse);

            builder.Property(v => v.RespiratoryRate);

            builder.Property(v => v.SPO2);

            builder.Property(v => v.Weight)
                .HasColumnType("decimal(5,2)");

            builder.Property(v => v.Height)
                .HasColumnType("decimal(5,2)");

            builder.Property(v => v.BMI)
                .HasColumnType("decimal(5,2)");

            // Examination
            builder.Property(v => v.GeneralExamination)
                .HasMaxLength(1000);

            builder.Property(v => v.SystemicExamination)
                .HasMaxLength(1000);

            builder.Property(v => v.LocalExamination)
                .HasMaxLength(1000);

            // Investigations & Advice
            builder.Property(v => v.InvestigationsOrdered)
                .HasMaxLength(2000);

            builder.Property(v => v.Advice)
                .HasMaxLength(1000);

            builder.Property(v => v.FollowUpInstructions)
                .HasMaxLength(500);

            builder.Property(v => v.FollowUpDate)
                .IsRequired(false);

            builder.Property(v => v.FollowUpDays);

            // Referrals
            builder.Property(v => v.ReferredToDoctorId)
                .IsRequired(false);

            builder.Property(v => v.ReferralReason)
                .HasMaxLength(500);

            // Payment Reference
            builder.Property(v => v.OPDBillId)
                .IsRequired(false);

            // Duration
            builder.Property(v => v.ConsultationDuration)
                .HasConversion<TimeSpan?>()
                .IsRequired(false);

            // Indexes
            builder.HasIndex(v => v.VisitNumber)
                .IsUnique();

            builder.HasIndex(v => v.PatientId);

            builder.HasIndex(v => v.DoctorId);

            builder.HasIndex(v => v.VisitDate);

            builder.HasIndex(v => v.Status);

            builder.HasIndex(v => v.FollowUpDate);

            // Navigation
            builder.HasOne(v => v.Patient)
                .WithMany(p => p.OPDVisits)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Doctor)
                .WithMany(d => d.OPDVisits)
                .HasForeignKey(v => v.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.ReferredToDoctor)
                .WithMany()
                .HasForeignKey(v => v.ReferredToDoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Prescription)
                .WithOne(p => p.OPDVisit)
                .HasForeignKey<Prescription>(p => p.OPDVisitId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Bill)
                .WithOne(b => b.OPDVisit)
                .HasForeignKey<OPDBill>(b => b.OPDVisitId)
                .OnDelete(DeleteBehavior.Restrict);           
        }       
    }
}