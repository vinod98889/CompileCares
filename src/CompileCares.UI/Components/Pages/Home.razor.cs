// Home.razor.cs
using CompileCares.Application.Features.Billing.DTOs;
using CompileCares.Application.Features.Consultations.DTOs;
using CompileCares.Application.Features.Patients.DTOs;
using CompileCares.Application.Features.Prescriptions.DTOs;
using CompileCares.Application.Features.Visits.DTOs;
using CompileCares.Core.Entities.Patients;
using CompileCares.Shared.Enums;
using CompileCares.UI.Services.AuthService;
using CompileCares.UI.Services.ConsultationService;
using CompileCares.UI.Services.ServerService;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Net.Http.Json;

namespace CompileCares.UI.Components.Pages
{
    public partial class Home : ComponentBase, IDisposable
    {
        [Inject]
        private IAuthStateService AuthStateService { get; set; } = default!;

        [Inject]
        private IHttpClientService HttpClientService { get; set; } = default!;

        [Inject]
        private IApiServerService ApiServerService { get; set; } = default!;

        [Inject]
        private IConsultationService ConsultationService { get; set; } = default!;

        [Inject]
        private NavigationManager Navigation { get; set; } = default!;

        private List<Patient> patients = new();
        private List<Patient> filteredPatients = new();

        // Consultation Models
        private CompleteConsultationRequest completeConsultationRequest = new();
        private QuickConsultationRequest quickConsultationRequest = new();

        // Patient Selection
        private Guid? selectedPatientId;
        private string patientSearchText = string.Empty;

        // Doctor Information
        private Guid currentDoctorId = Guid.NewGuid(); // This should come from auth service

        // Vitals
        private string bloodPressure = string.Empty;
        private decimal? temperature;
        private int? pulse;
        private string weight = string.Empty;
        private string spo2 = string.Empty;
        private string diagnosis = string.Empty;

        // Medicines
        private List<UltraQuickMedicine> medicines = new();
        private string medicineSearch = string.Empty;
        private Guid? selectedMedicineId;
        private Guid? selectedDoseId;
        private string instructions = string.Empty;
        private int durationDays = 5;
        private int quantity = 1;

        // Advice
        private List<string> selectedAdvice = new();
        private string adviceSearch = string.Empty;

        // Complaints
        private List<string> complaints = new();
        private string complaintInput = string.Empty;

        // Follow-up
        private bool setFollowUp = false;
        private int? followUpDays;
        private string followUpNotes = string.Empty;

        // Billing
        private decimal consultationFee = 0;
        private string paymentMode = "Cash";

        // UI State
        private bool isLoading = false;
        private bool isSaving = false;
        private string errorMessage = string.Empty;
        private string successMessage = string.Empty;
        private bool _disposed;

        //
        private string patientMobile = string.Empty;
        private string patientAddress = string.Empty;

        private bool isNewPatient = false;
        private string newPatientName = string.Empty;
        private string newPatientMobile = string.Empty;
        private Gender newPatientGender = Gender.Male;
        private string newPatientAge = string.Empty;
        private string newPatientAddress = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            // Subscribe to auth state changes
            AuthStateService.PropertyChanged += OnAuthStateChanged;

            // Initialize auth state
            await AuthStateService.InitializeAsync();

            if (AuthStateService.IsAuthenticated)
            {
                await LoadPatients();
                await InitializeDoctor();
            }
        }

        private async Task InitializeDoctor()
        {
            // TODO: Get current doctor ID from auth service
            // For now, using a placeholder
            currentDoctorId = Guid.Parse("35FB293E-B315-41EC-8CEA-E67070BFA2DA");

            // Load today's stats
            await LoadTodaysStats();
        }

        private async Task LoadTodaysStats()
        {
            try
            {
                var response = await ConsultationService.GetTodaysStatsAsync(currentDoctorId);
                if (response.Success && response.Data != null)
                {
                    Console.WriteLine($"Today's consultations: {response.Data.ConsultationCount}");
                    Console.WriteLine($"Today's revenue: {response.Data.TotalRevenue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading stats: {ex.Message}");
            }
        }

        private async void OnAuthStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuthStateService.IsAuthenticated))
            {
                if (!AuthStateService.IsAuthenticated)
                {
                    // If we become unauthenticated, redirect to login
                    Console.WriteLine("Auth state changed to unauthenticated");
                    Navigation.NavigateTo("/login", true);
                }
                else if (!isLoading && patients.Count == 0)
                {
                    // If we become authenticated, load patients
                    await LoadPatients();
                    await InitializeDoctor();
                }
            }

            // Re-render component when auth state changes
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadPatients()
        {
            try
            {
                isLoading = true;
                errorMessage = string.Empty;

                // Ensure we're authenticated
                if (!AuthStateService.IsAuthenticated)
                {
                    errorMessage = "Not authenticated";
                    return;
                }

                // Get authenticated HttpClient
                var httpClient = await HttpClientService.GetAuthenticatedClientAsync();

                if (!HttpClientService.IsAuthenticated)
                {
                    errorMessage = "Authentication failed";
                    return;
                }

                // Check API server
                if (!ApiServerService.IsRunning)
                {
                    Console.WriteLine("Starting API server");
                    ApiServerService.StartServer("http://localhost:7194");
                    await Task.Delay(2000);
                }

                // Make API call
                Console.WriteLine($"Loading patients from: {ApiServerService.ServerUrl}/api/v1.0/patient");

                var response = await httpClient.GetAsync($"{ApiServerService.ServerUrl}/api/v1.0/patient");

                if (response.IsSuccessStatusCode)
                {
                    patients = await response.Content.ReadFromJsonAsync<List<Patient>>() ?? new List<Patient>();
                    filteredPatients = patients;
                    Console.WriteLine($"Loaded {patients.Count} patients");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Token expired during API call
                    Console.WriteLine("API returned Unauthorized");
                    errorMessage = "Session expired";

                    // Logout and redirect
                    await AuthStateService.LogoutAsync();
                }
                else
                {
                    errorMessage = $"API Error: {response.StatusCode}";
                    Console.WriteLine($"API error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading patients: {ex.Message}";
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }
        private void CreateNewPatientFromSearch()
        {
            isNewPatient = true;
            newPatientName = patientSearchText;
            selectedPatientId = null; // Clear existing patient selection
            StateHasChanged();
        }

        private void SaveNewPatientDetails()
        {
            // Validate new patient data
            if (string.IsNullOrWhiteSpace(newPatientName))
            {
                errorMessage = "Please enter patient name";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPatientMobile))
            {
                errorMessage = "Please enter mobile number";
                return;
            }

            // Set the patient search text to show new patient
            patientSearchText = $"{newPatientName} (New Patient)";
            isNewPatient = false;
            errorMessage = string.Empty;
            StateHasChanged();
        }

        private void CancelNewPatient()
        {
            isNewPatient = false;
            newPatientName = string.Empty;
            newPatientMobile = string.Empty;
            newPatientAge = string.Empty;
            newPatientAddress = string.Empty;
            StateHasChanged();
        }
        private void SearchPatients()
        {
            if (string.IsNullOrWhiteSpace(patientSearchText))
            {
                filteredPatients = patients;
            }
            else
            {
                filteredPatients = patients.Where(p =>
                    (p.Name?.Contains(patientSearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Mobile?.Contains(patientSearchText) ?? false) ||
                    (p.PatientNumber?.Contains(patientSearchText, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }
        }

        private void SelectPatient(Patient patient)
        {
            selectedPatientId = patient.Id;
            patientSearchText = $"{patient.Name} ({patient.PatientNumber})";

            //
            patientMobile = patient.Mobile ?? string.Empty;
            patientAddress = patient.Address ?? string.Empty;

            // Pre-fill patient data
            if (completeConsultationRequest != null)
            {
                completeConsultationRequest.ExistingPatientId = patient.Id;
                completeConsultationRequest.IsNewPatient = false;
            }

            // Also update quick consultation request
            if (quickConsultationRequest != null)
            {
                quickConsultationRequest.PatientId = patient.Id;
                quickConsultationRequest.PatientName = patient.Name;
                quickConsultationRequest.PatientMobile = patient.Mobile;
            }

            StateHasChanged();
        }

        private void ClearPatientSelection()
        {
            selectedPatientId = null;
            patientSearchText = string.Empty;
            filteredPatients = patients;

            //
            patientMobile = string.Empty;
            patientAddress = string.Empty;

            if (completeConsultationRequest != null)
            {
                completeConsultationRequest.ExistingPatientId = null;
                completeConsultationRequest.IsNewPatient = true;
            }

            if (quickConsultationRequest != null)
            {
                quickConsultationRequest.PatientId = null;
                quickConsultationRequest.PatientName = string.Empty;
                quickConsultationRequest.PatientMobile = string.Empty;
            }
        }

        private void AddComplaint()
        {
            if (!string.IsNullOrWhiteSpace(complaintInput))
            {
                complaints.Add(complaintInput.Trim());
                complaintInput = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveComplaint(int index)
        {
            if (index >= 0 && index < complaints.Count)
            {
                complaints.RemoveAt(index);
                StateHasChanged();
            }
        }

        private void AddMedicine()
        {
            if (selectedMedicineId.HasValue && selectedDoseId.HasValue)
            {
                var medicine = new UltraQuickMedicine
                {
                    MedicineId = selectedMedicineId.Value,
                    DoseId = selectedDoseId.Value,
                    DurationDays = durationDays,
                    Quantity = quantity,
                    Instructions = instructions
                };

                medicines.Add(medicine);

                // Reset form
                selectedMedicineId = null;
                selectedDoseId = null;
                instructions = string.Empty;
                durationDays = 5;
                quantity = 1;
                medicineSearch = string.Empty;

                StateHasChanged();
            }
        }

        private void RemoveMedicine(int index)
        {
            if (index >= 0 && index < medicines.Count)
            {
                medicines.RemoveAt(index);
                StateHasChanged();
            }
        }

        private void AddAdvice()
        {
            if (!string.IsNullOrWhiteSpace(adviceSearch))
            {
                selectedAdvice.Add(adviceSearch.Trim());
                adviceSearch = string.Empty;
                StateHasChanged();
            }
        }

        private void RemoveAdvice(int index)
        {
            if (index >= 0 && index < selectedAdvice.Count)
            {
                selectedAdvice.RemoveAt(index);
                StateHasChanged();
            }
        }

        private async Task SavePrescription()
        {
            try
            {
                isSaving = true;
                errorMessage = string.Empty;
                successMessage = string.Empty;

                // Validate - Check if patient is selected or new patient is being created
                if (!selectedPatientId.HasValue && !isNewPatient && string.IsNullOrEmpty(newPatientName))
                {
                    errorMessage = "Please select a patient or enter new patient details";
                    return;
                }

                if (string.IsNullOrWhiteSpace(diagnosis))
                {
                    errorMessage = "Please enter a diagnosis";
                    return;
                }

                if (consultationFee <= 0)
                {
                    errorMessage = "Please enter a consultation fee";
                    return;
                }

                // Build complete consultation request
                var request = new CompleteConsultationRequest
                {
                    // Set IsNewPatient based on whether we have an existing patient ID
                    IsNewPatient = !selectedPatientId.HasValue,
                    ExistingPatientId = selectedPatientId,

                    // If new patient, create the patient object
                    NewPatient = !selectedPatientId.HasValue ? new PatientQuickCreateRequest
                    {
                        Name = newPatientName,
                        Mobile = newPatientMobile,
                        Gender = newPatientGender,
                        Age = int.TryParse(newPatientAge, out int age) ? age : 0,
                        Address = newPatientAddress
                    } : null,

                    DoctorId = currentDoctorId,
                    ConsultationDetails = new UpdateVisitRequest
                    {
                        ChiefComplaint = string.Join(", ", complaints),
                        Diagnosis = diagnosis,
                        // Add vitals if available
                        BloodPressure = bloodPressure,
                        Temperature = temperature,
                        Pulse = pulse,
                        //Weight = weight,
                        //SpO2 = spo2
                    },
                    Medicines = medicines.Select(m => new AddMedicineRequest
                    {
                        MedicineId = m.MedicineId,
                        DoseId = m.DoseId,
                        DurationDays = m.DurationDays,
                        Quantity = m.Quantity,
                        Instructions = m.Instructions
                    }).ToList(),
                    Advice = selectedAdvice.Select(a => new PrescriptionAdvisedRequest
                    {
                        CustomAdvice = a
                    }).ToList(),
                    ConsultationFee = consultationFee,
                    DiscountPercentage = 0,
                    TaxPercentage = 5.0m,
                    Payment = new AddPaymentRequest
                    {
                        Amount = consultationFee,
                        PaymentMode = paymentMode,
                    },
                    SetFollowUp = setFollowUp,
                    FollowUpDays = followUpDays,
                    FollowUpInstructions = followUpNotes,
                    ConsultationNotes = followUpNotes,
                    OverrideExisting = false,
                    AllowMultipleVisitsPerDay = true
                };

                // Call consultation service
                var response = await ConsultationService.CompleteConsultationAsync(request);

                if (response.Success && response.Data != null)
                {
                    successMessage = "Prescription saved successfully!";

                    // If it was a new patient, reload patients list
                    if (!selectedPatientId.HasValue)
                    {
                        await LoadPatients();
                    }

                    // Reset form
                    await ResetForm();

                    // Reload today's stats
                    await LoadTodaysStats();
                }
                else
                {
                    errorMessage = response.Message ?? "Failed to save prescription";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error saving prescription: {ex.Message}";
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private async Task SaveAndPrint()
        {
            await SavePrescription();

            if (string.IsNullOrEmpty(errorMessage))
            {
                // TODO: Implement print functionality
                Console.WriteLine("Printing prescription...");

                // You can use the ConsultationResult from the save operation
                // to generate print URLs or trigger print
            }
        }

        private async Task PrintBill()
        {
            if (!selectedPatientId.HasValue)
            {
                errorMessage = "Please select a patient first";
                return;
            }

            // TODO: Implement bill printing
            Console.WriteLine("Printing bill...");
        }

        private async Task ResetForm()
        {
            // Clear all form fields
            selectedPatientId = null;
            patientSearchText = string.Empty;
            complaints.Clear();
            complaintInput = string.Empty;
            medicines.Clear();
            selectedAdvice.Clear();
            adviceSearch = string.Empty;
            bloodPressure = string.Empty;
            temperature = null;
            pulse = null;
            weight = string.Empty;
            spo2 = string.Empty;
            diagnosis = string.Empty;
            setFollowUp = false;
            followUpDays = null;
            followUpNotes = string.Empty;
            consultationFee = 0;
            paymentMode = "Cash";

            // Reset requests
            completeConsultationRequest = new CompleteConsultationRequest();
            quickConsultationRequest = new QuickConsultationRequest();

            StateHasChanged();
        }

        private async Task QuickConsultation()
        {
            try
            {
                isSaving = true;
                errorMessage = string.Empty;
                successMessage = string.Empty;

                // Validate
                if (!selectedPatientId.HasValue)
                {
                    errorMessage = "Please select a patient";
                    return;
                }

                // Build quick consultation request
                var request = new QuickConsultationRequest
                {
                    PatientId = selectedPatientId.Value,
                    DoctorId = currentDoctorId,
                    ChiefComplaint = string.Join(", ", complaints),
                    Diagnosis = diagnosis,
                    MedicineCodes = new List<string>(), // TODO: Convert medicines to codes
                    AdviceCodes = selectedAdvice,
                    Fee = consultationFee,
                    PaymentMode = paymentMode,
                    FollowUpDays = followUpDays
                };

                // Call consultation service
                var response = await ConsultationService.QuickConsultationAsync(request);

                if (response.Success && response.Data != null)
                {
                    successMessage = "Quick consultation completed successfully!";

                    // Reset form
                    await ResetForm();

                    // Reload today's stats
                    await LoadTodaysStats();
                }
                else
                {
                    errorMessage = response.Message ?? "Failed to complete quick consultation";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error in quick consultation: {ex.Message}";
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private async Task UltraQuickConsultation()
        {
            try
            {
                isSaving = true;
                errorMessage = string.Empty;
                successMessage = string.Empty;

                // Validate
                if (!selectedPatientId.HasValue)
                {
                    errorMessage = "Please select a patient";
                    return;
                }

                // Build ultra-quick consultation request
                var request = new UltraQuickConsultationRequest
                {
                    IsNewPatient = false,
                    ExistingPatientId = selectedPatientId.Value,
                    DoctorId = currentDoctorId,
                    ChiefComplaint = string.Join(", ", complaints),
                    Diagnosis = diagnosis,
                    BloodPressure = bloodPressure,
                    Temperature = temperature,
                    Pulse = pulse,
                    Medicines = medicines,
                    ConsultationFee = consultationFee,
                    SetFollowUp = setFollowUp,
                    FollowUpDays = followUpDays,
                    FollowUpInstructions = followUpNotes
                };

                // Call consultation service
                var response = await ConsultationService.UltraQuickConsultationAsync(request);

                if (response.Success && response.Data != null)
                {
                    successMessage = "Ultra-quick consultation completed successfully!";

                    // Reset form
                    await ResetForm();

                    // Reload today's stats
                    await LoadTodaysStats();
                }
                else
                {
                    errorMessage = response.Message ?? "Failed to complete ultra-quick consultation";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error in ultra-quick consultation: {ex.Message}";
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private async Task RefreshData()
        {
            await LoadPatients();
            await LoadTodaysStats();
        }

        private async Task Logout()
        {
            await AuthStateService.LogoutAsync();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                AuthStateService.PropertyChanged -= OnAuthStateChanged;
                _disposed = true;
            }
        }
    }
}