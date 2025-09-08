using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using LiveCharts.WinForms;
using Timer = System.Windows.Forms.Timer;

namespace AutoGuru;

public partial class Form1 : Form
{
    // UI Components
    private Panel leftPanel;
    private Panel rightPanel;
    private Panel bottomPanel;
    
    private ComboBox engineCCComboBox;
    private ComboBox transmissionComboBox;
    private TrackBar throttleTrackBar;
    private Button startStopButton;
    private Button gearUpButton;
    private Button gearDownButton;
    
    private LiveCharts.WinForms.CartesianChart rpmChart;
    private LiveCharts.WinForms.CartesianChart torqueHpChart;
    
    private Label rpmLabel;
    private Label torqueLabel;
    private Label horsepowerLabel;
    private Label gearLabel;
    
    // Simulation components
    private Timer simulationTimer;
    private EngineModel engineModel;
    private bool isSimulationRunning = false;
    private List<double> timeData = new List<double>();
    private List<double> rpmData = new List<double>();
    private List<double> torqueData = new List<double>();
    private List<double> horsepowerData = new List<double>();
    private double simulationTime = 0;
    
    public Form1()
    {
        InitializeComponent();
        InitializeUI();
        InitializeCharts();
        InitializeSimulation();
        
        // Set up key event handling for gear changes
        this.KeyPreview = true;
        this.KeyDown += Form1_KeyDown;
        
        // Set form title
        this.Text = "AutoGuru - Engine & Transmission Simulator";
    }
    
    private void InitializeUI()
    {
        // Configure main form
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Create panels
        leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 300,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(240, 240, 240)
        };
        
        rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        
        bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(230, 230, 230)
        };
        
        // Add panels to form
        this.Controls.Add(rightPanel);
        this.Controls.Add(bottomPanel);
        this.Controls.Add(leftPanel);
        
        // Create left panel controls
        Label engineCCLabel = new Label
        {
            Text = "Engine CC:",
            AutoSize = true,
            Location = new Point(10, 20)
        };
        
        engineCCComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(10, 45),
            Width = 280,
            Font = new Font("Segoe UI", 10)
        };
        engineCCComboBox.Items.AddRange(new object[] { "800", "1000", "1200", "1500", "2000" });
        engineCCComboBox.SelectedIndex = 2; // Default to 1200cc
        engineCCComboBox.SelectedIndexChanged += EngineCC_SelectedIndexChanged;
        
        Label transmissionLabel = new Label
        {
            Text = "Transmission Type:",
            AutoSize = true,
            Location = new Point(10, 80)
        };
        
        transmissionComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(10, 105),
            Width = 280,
            Font = new Font("Segoe UI", 10)
        };
        transmissionComboBox.Items.AddRange(new object[] { "Manual", "AMT", "IMT", "CVT", "DCT" });
        transmissionComboBox.SelectedIndex = 0; // Default to Manual
        transmissionComboBox.SelectedIndexChanged += Transmission_SelectedIndexChanged;
        
        Label throttleLabel = new Label
        {
            Text = "Throttle Input:",
            AutoSize = true,
            Location = new Point(10, 140)
        };
        
        throttleTrackBar = new TrackBar
        {
            Location = new Point(10, 165),
            Width = 280,
            Minimum = 0,
            Maximum = 100,
            TickFrequency = 10,
            LargeChange = 10,
            SmallChange = 5,
            Value = 0
        };
        throttleTrackBar.ValueChanged += Throttle_ValueChanged;
        
        Label throttleValueLabel = new Label
        {
            Text = "0%",
            AutoSize = true,
            Location = new Point(130, 205),
            Name = "throttleValueLabel"
        };
        
        startStopButton = new Button
        {
            Text = "Start Simulation",
            Location = new Point(10, 235),
            Width = 280,
            Height = 40,
            BackColor = Color.FromArgb(92, 184, 92),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };
        startStopButton.FlatAppearance.BorderSize = 0;
        startStopButton.Click += StartStop_Click;
        
        gearUpButton = new Button
        {
            Text = "Gear Up ↑",
            Location = new Point(10, 285),
            Width = 135,
            Height = 40,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            FlatStyle = FlatStyle.Flat,
            Visible = true // Only visible for Manual transmission
        };
        gearUpButton.FlatAppearance.BorderSize = 0;
        gearUpButton.Click += GearUp_Click;
        
        gearDownButton = new Button
        {
            Text = "Gear Down ↓",
            Location = new Point(155, 285),
            Width = 135,
            Height = 40,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10),
            FlatStyle = FlatStyle.Flat,
            Visible = true // Only visible for Manual transmission
        };
        gearDownButton.FlatAppearance.BorderSize = 0;
        gearDownButton.Click += GearDown_Click;
        
        // Add controls to left panel
        leftPanel.Controls.Add(engineCCLabel);
        leftPanel.Controls.Add(engineCCComboBox);
        leftPanel.Controls.Add(transmissionLabel);
        leftPanel.Controls.Add(transmissionComboBox);
        leftPanel.Controls.Add(throttleLabel);
        leftPanel.Controls.Add(throttleTrackBar);
        leftPanel.Controls.Add(throttleValueLabel);
        leftPanel.Controls.Add(startStopButton);
        leftPanel.Controls.Add(gearUpButton);
        leftPanel.Controls.Add(gearDownButton);
        
        // Create bottom panel controls (information display)
        rpmLabel = new Label
        {
            Text = "RPM: 0",
            AutoSize = true,
            Location = new Point(20, 20),
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        
        torqueLabel = new Label
        {
            Text = "Torque: 0 Nm",
            AutoSize = true,
            Location = new Point(200, 20),
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        
        horsepowerLabel = new Label
        {
            Text = "Horsepower: 0 HP",
            AutoSize = true,
            Location = new Point(400, 20),
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        
        gearLabel = new Label
        {
            Text = "Gear: N",
            AutoSize = true,
            Location = new Point(650, 20),
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        
        // Add controls to bottom panel
        bottomPanel.Controls.Add(rpmLabel);
        bottomPanel.Controls.Add(torqueLabel);
        bottomPanel.Controls.Add(horsepowerLabel);
        bottomPanel.Controls.Add(gearLabel);
        
        // Create charts for right panel
        TableLayoutPanel chartsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            RowStyles = { new RowStyle(SizeType.Percent, 50), new RowStyle(SizeType.Percent, 50) }
        };
        
        rpmChart = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10)
        };
        
        torqueHpChart = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 10, 0, 0)
        };
        
        chartsPanel.Controls.Add(rpmChart, 0, 0);
        chartsPanel.Controls.Add(torqueHpChart, 0, 1);
        rightPanel.Controls.Add(chartsPanel);
    }
    
    private void InitializeCharts()
    {
        // Configure RPM vs Time chart
        rpmChart.Series = new SeriesCollection
        {
            new LiveCharts.Wpf.LineSeries
            {
                Title = "RPM",
                Values = new ChartValues<double>(),
                PointGeometry = null,
                LineSmoothness = 0.5,
                StrokeThickness = 2
            }
        };
        
        rpmChart.AxisX.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Time (s)",
            LabelFormatter = value => value.ToString("F1")
        });
        
        rpmChart.AxisY.Add(new LiveCharts.Wpf.Axis
        {
            Title = "RPM",
            MinValue = 0,
            MaxValue = 8000
        });
        
        rpmChart.LegendLocation = LegendLocation.Top;
        
        // Configure Torque & Horsepower vs RPM chart
        torqueHpChart.Series = new SeriesCollection
        {
            new LiveCharts.Wpf.LineSeries
            {
                Title = "Torque (Nm)",
                Values = new ChartValues<double>(),
                PointGeometry = null,
                LineSmoothness = 0.5,
                StrokeThickness = 2
            },
            new LiveCharts.Wpf.LineSeries
            {
                Title = "Horsepower (HP)",
                Values = new ChartValues<double>(),
                PointGeometry = null,
                LineSmoothness = 0.5,
                StrokeThickness = 2
            }
        };
        
        torqueHpChart.AxisX.Add(new LiveCharts.Wpf.Axis
        {
            Title = "RPM",
            MinValue = 0,
            MaxValue = 8000
        });
        
        torqueHpChart.AxisY.Add(new LiveCharts.Wpf.Axis
        {
            Title = "Torque (Nm) / Horsepower (HP)",
            MinValue = 0
        });
        
        torqueHpChart.LegendLocation = LegendLocation.Top;
    }
    
    private void InitializeSimulation()
    {
        // Create engine model
        engineModel = new EngineModel
        {
            EngineCC = int.Parse(engineCCComboBox.SelectedItem?.ToString() ?? "1200"),
            TransmissionType = transmissionComboBox.SelectedItem?.ToString() ?? "Manual",
            CurrentRPM = 800, // Idle RPM
            CurrentGear = 0, // Neutral
            ThrottleInput = 0
        };
        
        // Create simulation timer
        simulationTimer = new Timer
        {
            Interval = 100 // Update every 100ms
        };
        simulationTimer.Tick += new EventHandler(SimulationTimer_Tick);
        
        // Update gear buttons visibility based on transmission type
        UpdateGearButtonsVisibility();
    }
    
    private void UpdateGearButtonsVisibility()
    {
        bool isManual = transmissionComboBox.SelectedItem?.ToString() == "Manual";
        gearUpButton.Visible = isManual;
        gearDownButton.Visible = isManual;
    }
    
    private void StartStop_Click(object sender, EventArgs e)
    {
        if (isSimulationRunning)
        {
            // Stop simulation
            simulationTimer.Stop();
            isSimulationRunning = false;
            startStopButton.Text = "Start Simulation";
            startStopButton.BackColor = Color.FromArgb(92, 184, 92); // Green
            
            // Reset engine to idle
            engineModel.CurrentRPM = 800;
            engineModel.CurrentGear = 0; // Neutral
            engineModel.ThrottleInput = 0;
            throttleTrackBar.Value = 0;
            UpdateDisplayLabels();
        }
        else
        {
            // Start simulation
            simulationTimer.Start();
            isSimulationRunning = true;
            startStopButton.Text = "Stop Simulation";
            startStopButton.BackColor = Color.FromArgb(217, 83, 79); // Red
            
            // Clear previous data
            timeData.Clear();
            rpmData.Clear();
            torqueData.Clear();
            horsepowerData.Clear();
            simulationTime = 0;
            
            // Clear charts
            rpmChart.Series[0].Values.Clear();
            torqueHpChart.Series[0].Values.Clear();
            torqueHpChart.Series[1].Values.Clear();
        }
    }
    
    private void GearUp_Click(object sender, EventArgs e)
    {
        if (isSimulationRunning && engineModel.CurrentGear < 6) // Max 6 gears
        {
            engineModel.CurrentGear++;
            UpdateDisplayLabels();
        }
    }
    
    private void GearDown_Click(object sender, EventArgs e)
    {
        if (isSimulationRunning && engineModel.CurrentGear > 0) // Min Neutral (0)
        {
            engineModel.CurrentGear--;
            UpdateDisplayLabels();
        }
    }
    
    private void Form1_KeyDown(object sender, KeyEventArgs e)
    {
        // Handle keyboard gear changes for Manual transmission
        if (transmissionComboBox.SelectedItem?.ToString() == "Manual" && isSimulationRunning)
        {
            if (e.KeyCode == Keys.Up)
            {
                GearUp_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                GearDown_Click(sender, e);
                e.Handled = true;
            }
        }
    }
    
    private void Throttle_ValueChanged(object sender, EventArgs e)
    {
        // Update throttle value label
        if (leftPanel.Controls["throttleValueLabel"] is Label throttleValueLabel)
        {
            throttleValueLabel.Text = $"{throttleTrackBar.Value}%";
        }
        
        // Update engine model throttle input
        engineModel.ThrottleInput = throttleTrackBar.Value;
    }
    
    private void EngineCC_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (engineModel != null)
        {
            engineModel.EngineCC = int.Parse(engineCCComboBox.SelectedItem?.ToString() ?? "1200");
        }
    }
    
    private void Transmission_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (engineModel != null)
        {
            engineModel.TransmissionType = transmissionComboBox.SelectedItem?.ToString() ?? "Manual";
            UpdateGearButtonsVisibility();
        }
    }
    
    private void SimulationTimer_Tick(object sender, EventArgs e)
    {
        // Update simulation time
        simulationTime += 0.1; // 100ms = 0.1s
        
        // Update engine model
        engineModel.UpdateSimulation(engineModel.ThrottleInput, 0.1);
        
        // Update display labels
        UpdateDisplayLabels();
        
        // Update charts
        UpdateCharts();
    }
    
    private void UpdateDisplayLabels()
    {
        // Update information labels
        rpmLabel.Text = $"RPM: {engineModel.CurrentRPM:F0}";
        
        double torque = engineModel.GetTorque(engineModel.CurrentRPM);
        torqueLabel.Text = $"Torque: {torque:F1} Nm";
        
        double horsepower = engineModel.GetHorsePower(engineModel.CurrentRPM, torque);
        horsepowerLabel.Text = $"Horsepower: {horsepower:F1} HP";
        
        string gearText = engineModel.CurrentGear == 0 ? "N" : engineModel.CurrentGear.ToString();
        gearLabel.Text = $"Gear: {gearText}";
    }
    
    private void UpdateCharts()
    {
        // Add data points
        timeData.Add(simulationTime);
        rpmData.Add(engineModel.CurrentRPM);
        
        double torque = engineModel.GetTorque(engineModel.CurrentRPM);
        torqueData.Add(torque);
        
        double horsepower = engineModel.GetHorsePower(engineModel.CurrentRPM, torque);
        horsepowerData.Add(horsepower);
        
        // Update RPM vs Time chart
        rpmChart.Series[0].Values.Add(engineModel.CurrentRPM);
        
        // Limit data points to keep performance good
        if (rpmChart.Series[0].Values.Count > 100)
        {
            rpmChart.Series[0].Values.RemoveAt(0);
        }
        
        // Update X-axis range for RPM chart
        if (timeData.Count > 0)
        {
            double minTime = timeData.Count > 100 ? timeData[timeData.Count - 100] : 0;
            double maxTime = timeData[timeData.Count - 1];
            rpmChart.AxisX[0].MinValue = minTime;
            rpmChart.AxisX[0].MaxValue = maxTime;
        }
        
        // Update Torque & Horsepower vs RPM chart
        // This is a bit different - we want to show the curves, not time series
        torqueHpChart.Series[0].Values.Clear();
        torqueHpChart.Series[1].Values.Clear();
        
        // Generate points for the curves
        for (int rpm = 800; rpm <= 8000; rpm += 200)
        {
            double t = engineModel.GetTorque(rpm);
            double hp = engineModel.GetHorsePower(rpm, t);
            
            torqueHpChart.Series[0].Values.Add(t);
            torqueHpChart.Series[1].Values.Add(hp);
        }
    }
    
    // Engine Model class (inside Form1 as requested)
    private class EngineModel
    {
        // Properties
        public int EngineCC { get; set; }
        public double CurrentRPM { get; set; }
        public int CurrentGear { get; set; }
        public int ThrottleInput { get; set; }
        public string TransmissionType { get; set; } = "Manual";
        
        // Gear ratios (simplified)
        private readonly double[] gearRatios = { 0, 3.909, 2.056, 1.269, 0.906, 0.779, 0.651 };
        
        // Shift points for automatic transmissions
        private readonly double[] upshiftRPM = { 0, 2500, 3000, 3500, 4000, 4500, 5000 };
        private readonly double[] downshiftRPM = { 0, 800, 1200, 1500, 1800, 2000, 2200 };
        
        // Transmission-specific parameters
        private double shiftDelay = 0; // Counts down during gear shifts
        private double cvtRatio = 2.0; // For CVT simulation
        private Random random = new Random(); // For adding noise
        
        // Get torque based on RPM and engine CC
        public double GetTorque(double rpm)
        {
            // Base torque values scaled by engine size
            double baseTorque = EngineCC / 1000.0 * 100; // Base scaling factor
            
            // Create a realistic torque curve that peaks at mid-range RPM
            double normalizedRPM = rpm / 8000.0; // Normalize RPM to 0-1 range
            double torqueFactor;
            
            // Different torque curves based on engine size
            switch (EngineCC)
            {
                case 800: // Small engine - early torque peak
                    torqueFactor = 4.0 * normalizedRPM * (1 - normalizedRPM) * (1 + 0.2 * normalizedRPM);
                    break;
                case 1000:
                    torqueFactor = 4.1 * normalizedRPM * (1 - normalizedRPM) * (1 + 0.3 * normalizedRPM);
                    break;
                case 1200:
                    torqueFactor = 4.2 * normalizedRPM * (1 - normalizedRPM) * (1 + 0.4 * normalizedRPM);
                    break;
                case 1500: // Mid-size engine - balanced torque
                    torqueFactor = 4.3 * normalizedRPM * (1 - normalizedRPM) * (1 + 0.5 * normalizedRPM);
                    break;
                case 2000: // Large engine - later torque peak
                    torqueFactor = 4.4 * normalizedRPM * (1 - normalizedRPM) * (1 + 0.6 * normalizedRPM);
                    break;
                default:
                    torqueFactor = 4.0 * normalizedRPM * (1 - normalizedRPM);
                    break;
            }
            
            // Calculate torque with some noise for realism
            double torque = baseTorque * torqueFactor;
            
            // Add some noise for realism (±2%)
            torque *= (1 + (random.NextDouble() - 0.5) * 0.04);
            
            // Scale by throttle input
            torque *= (0.2 + 0.8 * (ThrottleInput / 100.0));
            
            return Math.Max(0, torque); // Ensure non-negative
        }
        
        // Calculate horsepower from torque and RPM
        public double GetHorsePower(double rpm, double torque)
        {
            // Standard formula: HP = (Torque * RPM) / 5252
            return (torque * rpm) / 5252.0;
        }
        
        // Update simulation state based on inputs and time step
        public void UpdateSimulation(int throttle, double timestep)
        {
            // Handle gear shifts for automatic transmissions
            if (TransmissionType != "Manual" && CurrentGear > 0)
            {
                HandleAutomaticTransmission();
            }
            
            // If we're in a gear shift for automatic transmissions, handle the delay
            if (shiftDelay > 0)
            {
                shiftDelay -= timestep;
                // During shift, RPM changes based on transmission type
                if (TransmissionType == "DCT")
                {
                    // DCT shifts very quickly
                    CurrentRPM = CalculateRPMFromVehicleSpeed();
                }
                else if (TransmissionType == "AMT" || TransmissionType == "IMT")
                {
                    // AMT/IMT have a noticeable shift delay with RPM drop
                    CurrentRPM = Math.Max(800, CurrentRPM - 500 * timestep);
                }
                return; // Skip regular RPM calculation during shift
            }
            
            // Calculate target RPM based on throttle, gear, and transmission type
            double targetRPM;
            
            if (CurrentGear == 0) // Neutral
            {
                // In neutral, engine RPM is controlled directly by throttle
                targetRPM = 800 + (throttle / 100.0) * 7200; // Idle to redline
                
                // Approach target RPM gradually
                double rpmDiff = targetRPM - CurrentRPM;
                CurrentRPM += rpmDiff * 2 * timestep; // Faster response in neutral
            }
            else // In gear
            {
                if (TransmissionType == "CVT")
                {
                    // CVT keeps RPM at optimal point based on throttle
                    double optimalRPM = 1500 + (throttle / 100.0) * 2500;
                    targetRPM = optimalRPM;
                    
                    // CVT adjusts ratio to maintain optimal RPM
                    cvtRatio = Math.Max(0.5, Math.Min(2.5, cvtRatio));
                    
                    // Smooth RPM changes for CVT
                    double rpmDiff = targetRPM - CurrentRPM;
                    CurrentRPM += rpmDiff * timestep;
                }
                else
                {
                    // Calculate RPM based on vehicle speed and gear ratio
                    targetRPM = CalculateRPMFromVehicleSpeed();
                    
                    // Apply throttle effect on acceleration
                    double accelerationFactor = (throttle / 100.0) * GetTorque(CurrentRPM) / (1000 * gearRatios[CurrentGear]);
                    double rpmChange = accelerationFactor * 5000 * timestep;
                    
                    // Apply engine braking when throttle is low
                    if (throttle < 10)
                    {
                        rpmChange -= (10 - throttle) * 10 * timestep;
                    }
                    
                    // Update RPM
                    CurrentRPM = Math.Max(800, CurrentRPM + rpmChange);
                }
            }
            
            // Add some jitter for realism
            CurrentRPM += (random.NextDouble() - 0.5) * 50;
            
            // Ensure RPM stays within limits
            CurrentRPM = Math.Max(800, Math.Min(8000, CurrentRPM));
        }
        
        // Handle automatic transmission gear shifts
        private void HandleAutomaticTransmission()
        {
            // Skip if we're already in a gear shift
            if (shiftDelay > 0) return;
            
            // Check for upshift
            if (CurrentRPM > upshiftRPM[CurrentGear] && CurrentGear < 6)
            {
                // Initiate upshift
                CurrentGear++;
                
                // Set shift delay based on transmission type
                switch (TransmissionType)
                {
                    case "DCT":
                        shiftDelay = 0.1; // Very fast shifts
                        break;
                    case "AMT":
                        shiftDelay = 0.5; // Slower shifts
                        break;
                    case "IMT":
                        shiftDelay = 0.3; // Medium shifts
                        break;
                }
            }
            // Check for downshift
            else if (CurrentRPM < downshiftRPM[CurrentGear] && CurrentGear > 1)
            {
                // Initiate downshift
                CurrentGear--;
                
                // Set shift delay based on transmission type
                switch (TransmissionType)
                {
                    case "DCT":
                        shiftDelay = 0.1; // Very fast shifts
                        break;
                    case "AMT":
                        shiftDelay = 0.5; // Slower shifts
                        break;
                    case "IMT":
                        shiftDelay = 0.3; // Medium shifts
                        break;
                }
            }
        }
        
        // Calculate RPM based on simulated vehicle speed and current gear
        private double CalculateRPMFromVehicleSpeed()
        {
            // This is a simplified model
            // In a real car, RPM = (Vehicle Speed * Final Drive Ratio * Gear Ratio * 336) / Tire Diameter
            
            // We'll use a simplified approach where we estimate vehicle speed from current RPM
            // and then calculate new RPM based on that speed and the new gear ratio
            
            // Estimate vehicle speed (arbitrary units)
            double vehicleSpeed = CurrentRPM / (gearRatios[CurrentGear] * 100);
            
            // Calculate new RPM based on vehicle speed and current gear ratio
            double newRPM;
            
            if (TransmissionType == "CVT")
            {
                // CVT continuously adjusts the ratio for optimal RPM
                newRPM = vehicleSpeed * cvtRatio * 100;
            }
            else
            {
                // Fixed gear ratios for other transmissions
                newRPM = vehicleSpeed * gearRatios[CurrentGear] * 100;
            }
            
            return newRPM;
        }
    }
}
