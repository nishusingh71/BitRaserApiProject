using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using DSecureApi.Services;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Controller to monitor API's own CPU and Memory usage on Render
    /// Works on free tier - no Render API key needed
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RenderMetricsController : ControllerBase
    {
        private readonly ILogger<RenderMetricsController> _logger;
        private static DateTime _startTime = DateTime.UtcNow;
        private static readonly Process _currentProcess = Process.GetCurrentProcess();
        private static TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private static DateTime _lastCpuCheck = DateTime.UtcNow;
        private static double _lastCpuUsage = 0;

        public RenderMetricsController(ILogger<RenderMetricsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get current memory usage of the API
        /// </summary>
        [HttpGet("memory")]
        [ProducesResponseType(typeof(MemoryMetricsResponse), 200)]
        public IActionResult GetMemoryMetrics()
        {
            try
            {
                _currentProcess.Refresh();

                var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var privateMemoryMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
                var virtualMemoryMB = _currentProcess.VirtualMemorySize64 / (1024.0 * 1024.0);
                var pagedMemoryMB = _currentProcess.PagedMemorySize64 / (1024.0 * 1024.0);

                // GC (Garbage Collection) memory info
                var gcInfo = GC.GetGCMemoryInfo();
                var totalAvailableMemoryMB = gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0);
                var heapSizeMB = gcInfo.HeapSizeBytes / (1024.0 * 1024.0);
                var gcTotalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                // Render free tier has 512MB RAM limit
                const double renderFreeTierLimitMB = 512.0;
                var usagePercentage = (workingSetMB / renderFreeTierLimitMB) * 100;

                var response = new MemoryMetricsResponse
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    ProcessMemory = new ProcessMemoryInfo
                    {
                        WorkingSetMB = Math.Round(workingSetMB, 2),
                        PrivateMemoryMB = Math.Round(privateMemoryMB, 2),
                        VirtualMemoryMB = Math.Round(virtualMemoryMB, 2),
                        PagedMemoryMB = Math.Round(pagedMemoryMB, 2)
                    },
                    GCMemory = new GCMemoryInfo
                    {
                        TotalAvailableMemoryMB = Math.Round(totalAvailableMemoryMB, 2),
                        HeapSizeMB = Math.Round(heapSizeMB, 2),
                        TotalAllocatedMB = Math.Round(gcTotalMemoryMB, 2),
                        Gen0Collections = GC.CollectionCount(0),
                        Gen1Collections = GC.CollectionCount(1),
                        Gen2Collections = GC.CollectionCount(2)
                    },
                    RenderInfo = new RenderLimitInfo
                    {
                        FreeTierLimitMB = renderFreeTierLimitMB,
                        CurrentUsageMB = Math.Round(workingSetMB, 2),
                        UsagePercentage = Math.Round(usagePercentage, 2),
                        Status = usagePercentage > 80 ? "WARNING" : usagePercentage > 60 ? "MODERATE" : "OK"
                    }
                };

                _logger.LogDebug("Memory metrics: {WorkingSet}MB ({Percentage}%)", 
                    Math.Round(workingSetMB, 2), Math.Round(usagePercentage, 2));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting memory metrics");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get current CPU usage of the API
        /// </summary>
        [HttpGet("cpu")]
        [ProducesResponseType(typeof(CpuMetricsResponse), 200)]
        public IActionResult GetCpuMetrics()
        {
            try
            {
                _currentProcess.Refresh();

                // Calculate CPU usage
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = _currentProcess.TotalProcessorTime;
                
                var timeDiff = currentTime - _lastCpuCheck;
                var cpuTimeDiff = currentCpuTime - _lastTotalProcessorTime;
                
                double cpuUsage = 0;
                if (timeDiff.TotalMilliseconds > 0)
                {
                    // CPU usage = (CPU time used / wall time) * 100 / processor count
                    cpuUsage = (cpuTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds) * 100 / Environment.ProcessorCount;
                    _lastCpuUsage = cpuUsage;
                }
                else
                {
                    cpuUsage = _lastCpuUsage;
                }

                // Update last values for next call
                _lastTotalProcessorTime = currentCpuTime;
                _lastCpuCheck = currentTime;

                var uptime = DateTime.UtcNow - _startTime;

                var response = new CpuMetricsResponse
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    CpuInfo = new CpuUsageInfo
                    {
                        UsagePercentage = Math.Round(Math.Min(cpuUsage, 100), 2),
                        TotalProcessorTimeSeconds = Math.Round(_currentProcess.TotalProcessorTime.TotalSeconds, 2),
                        UserProcessorTimeSeconds = Math.Round(_currentProcess.UserProcessorTime.TotalSeconds, 2),
                        PrivilegedProcessorTimeSeconds = Math.Round(_currentProcess.PrivilegedProcessorTime.TotalSeconds, 2),
                        ProcessorCount = Environment.ProcessorCount,
                        Status = cpuUsage > 80 ? "HIGH" : cpuUsage > 50 ? "MODERATE" : "OK"
                    },
                    ProcessInfo = new ProcessInfo
                    {
                        ProcessId = _currentProcess.Id,
                        ProcessName = _currentProcess.ProcessName,
                        ThreadCount = _currentProcess.Threads.Count,
                        HandleCount = _currentProcess.HandleCount,
                        StartTime = _currentProcess.StartTime.ToUniversalTime(),
                        Uptime = new UptimeInfo
                        {
                            Days = uptime.Days,
                            Hours = uptime.Hours,
                            Minutes = uptime.Minutes,
                            Seconds = uptime.Seconds,
                            TotalMinutes = Math.Round(uptime.TotalMinutes, 2)
                        }
                    }
                };

                _logger.LogDebug("CPU metrics: {Usage}%", Math.Round(cpuUsage, 2));

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CPU metrics");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get both CPU and Memory metrics together
        /// </summary>
        [HttpGet("all")]
        [ProducesResponseType(typeof(AllMetricsResponse), 200)]
        public IActionResult GetAllMetrics()
        {
            try
            {
                _currentProcess.Refresh();

                // Memory
                var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var privateMemoryMB = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
                var gcTotalMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                
                const double renderFreeTierLimitMB = 512.0;
                var memoryUsagePercentage = (workingSetMB / renderFreeTierLimitMB) * 100;

                // CPU
                var currentTime = DateTime.UtcNow;
                var currentCpuTime = _currentProcess.TotalProcessorTime;
                var timeDiff = currentTime - _lastCpuCheck;
                var cpuTimeDiff = currentCpuTime - _lastTotalProcessorTime;
                
                double cpuUsage = 0;
                if (timeDiff.TotalMilliseconds > 0)
                {
                    cpuUsage = (cpuTimeDiff.TotalMilliseconds / timeDiff.TotalMilliseconds) * 100 / Environment.ProcessorCount;
                    _lastCpuUsage = cpuUsage;
                }
                else
                {
                    cpuUsage = _lastCpuUsage;
                }

                _lastTotalProcessorTime = currentCpuTime;
                _lastCpuCheck = currentTime;

                var uptime = DateTime.UtcNow - _startTime;

                var response = new AllMetricsResponse
                {
                    Success = true,
                    Timestamp = DateTime.UtcNow,
                    Platform = Environment.GetEnvironmentVariable("RENDER") != null ? "Render.com" : "Local/Other",
                    Summary = new MetricsSummary
                    {
                        MemoryUsageMB = Math.Round(workingSetMB, 2),
                        MemoryLimitMB = renderFreeTierLimitMB,
                        MemoryUsagePercentage = Math.Round(memoryUsagePercentage, 2),
                        MemoryStatus = memoryUsagePercentage > 80 ? "⚠️ WARNING" : memoryUsagePercentage > 60 ? "🔶 MODERATE" : "✅ OK",
                        CpuUsagePercentage = Math.Round(Math.Min(cpuUsage, 100), 2),
                        CpuStatus = cpuUsage > 80 ? "🔴 HIGH" : cpuUsage > 50 ? "🟡 MODERATE" : "🟢 OK",
                        ThreadCount = _currentProcess.Threads.Count,
                        UptimeMinutes = Math.Round(uptime.TotalMinutes, 2)
                    },
                    Memory = new MemoryDetails
                    {
                        WorkingSetMB = Math.Round(workingSetMB, 2),
                        PrivateMemoryMB = Math.Round(privateMemoryMB, 2),
                        GcAllocatedMB = Math.Round(gcTotalMemoryMB, 2),
                        GcGen0Collections = GC.CollectionCount(0),
                        GcGen1Collections = GC.CollectionCount(1),
                        GcGen2Collections = GC.CollectionCount(2)
                    },
                    Cpu = new CpuDetails
                    {
                        UsagePercentage = Math.Round(Math.Min(cpuUsage, 100), 2),
                        TotalProcessorTimeSeconds = Math.Round(_currentProcess.TotalProcessorTime.TotalSeconds, 2),
                        ProcessorCount = Environment.ProcessorCount
                    },
                    Process = new ProcessDetails
                    {
                        Id = _currentProcess.Id,
                        Name = _currentProcess.ProcessName,
                        Threads = _currentProcess.Threads.Count,
                        Handles = _currentProcess.HandleCount,
                        StartTimeUtc = _currentProcess.StartTime.ToUniversalTime()
                    },
                    Environment = new EnvironmentDetails
                    {
                        MachineName = Environment.MachineName,
                        OsDescription = RuntimeInformation.OSDescription,
                        DotNetVersion = RuntimeInformation.FrameworkDescription,
                        ProcessorCount = Environment.ProcessorCount,
                        Is64Bit = Environment.Is64BitProcess
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all metrics");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get quick health status with resource usage
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(QuickStatusResponse), 200)]
        public IActionResult GetQuickStatus()
        {
            try
            {
                _currentProcess.Refresh();

                var workingSetMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                const double limitMB = 512.0;
                var memoryPercentage = (workingSetMB / limitMB) * 100;

                var uptime = DateTime.UtcNow - _startTime;

                return Ok(new QuickStatusResponse
                {
                    Status = memoryPercentage > 85 ? "CRITICAL" : memoryPercentage > 70 ? "WARNING" : "HEALTHY",
                    MemoryMB = Math.Round(workingSetMB, 1),
                    MemoryPercent = Math.Round(memoryPercentage, 1),
                    Threads = _currentProcess.Threads.Count,
                    UptimeMinutes = (int)uptime.TotalMinutes,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Ok(new QuickStatusResponse
                {
                    Status = "ERROR",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Force garbage collection and get before/after memory stats
        /// Use carefully - this can impact performance temporarily
        /// </summary>
        [HttpPost("gc")]
        [ProducesResponseType(typeof(GcResultResponse), 200)]
        public IActionResult ForceGarbageCollection()
        {
            try
            {
                _currentProcess.Refresh();
                var beforeMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var beforeGcMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

                // Force full garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _currentProcess.Refresh();
                var afterMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);
                var afterGcMB = GC.GetTotalMemory(true) / (1024.0 * 1024.0);

                var freedMB = beforeMB - afterMB;
                var freedGcMB = beforeGcMB - afterGcMB;

                _logger.LogInformation("GC forced: Freed {FreedMB}MB working set, {FreedGcMB}MB GC memory", 
                    Math.Round(freedMB, 2), Math.Round(freedGcMB, 2));

                return Ok(new GcResultResponse
                {
                    Success = true,
                    Message = "Garbage collection completed",
                    Before = new MemorySnapshot
                    {
                        WorkingSetMB = Math.Round(beforeMB, 2),
                        GcAllocatedMB = Math.Round(beforeGcMB, 2)
                    },
                    After = new MemorySnapshot
                    {
                        WorkingSetMB = Math.Round(afterMB, 2),
                        GcAllocatedMB = Math.Round(afterGcMB, 2)
                    },
                    Freed = new MemorySnapshot
                    {
                        WorkingSetMB = Math.Round(Math.Max(freedMB, 0), 2),
                        GcAllocatedMB = Math.Round(Math.Max(freedGcMB, 0), 2)
                    },
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during garbage collection");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }

    #region Response Models

    public class MemoryMetricsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("processMemory")]
        public ProcessMemoryInfo ProcessMemory { get; set; } = new();
        
        [JsonPropertyName("gcMemory")]
        public GCMemoryInfo GCMemory { get; set; } = new();
        
        [JsonPropertyName("renderInfo")]
        public RenderLimitInfo RenderInfo { get; set; } = new();
    }

    public class ProcessMemoryInfo
    {
        [JsonPropertyName("workingSetMB")]
        public double WorkingSetMB { get; set; }
        
        [JsonPropertyName("privateMemoryMB")]
        public double PrivateMemoryMB { get; set; }
        
        [JsonPropertyName("virtualMemoryMB")]
        public double VirtualMemoryMB { get; set; }
        
        [JsonPropertyName("pagedMemoryMB")]
        public double PagedMemoryMB { get; set; }
    }

    public class GCMemoryInfo
    {
        [JsonPropertyName("totalAvailableMemoryMB")]
        public double TotalAvailableMemoryMB { get; set; }
        
        [JsonPropertyName("heapSizeMB")]
        public double HeapSizeMB { get; set; }
        
        [JsonPropertyName("totalAllocatedMB")]
        public double TotalAllocatedMB { get; set; }
        
        [JsonPropertyName("gen0Collections")]
        public int Gen0Collections { get; set; }
        
        [JsonPropertyName("gen1Collections")]
        public int Gen1Collections { get; set; }
        
        [JsonPropertyName("gen2Collections")]
        public int Gen2Collections { get; set; }
    }

    public class RenderLimitInfo
    {
        [JsonPropertyName("freeTierLimitMB")]
        public double FreeTierLimitMB { get; set; }
        
        [JsonPropertyName("currentUsageMB")]
        public double CurrentUsageMB { get; set; }
        
        [JsonPropertyName("usagePercentage")]
        public double UsagePercentage { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class CpuMetricsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("cpuInfo")]
        public CpuUsageInfo CpuInfo { get; set; } = new();
        
        [JsonPropertyName("processInfo")]
        public ProcessInfo ProcessInfo { get; set; } = new();
    }

    public class CpuUsageInfo
    {
        [JsonPropertyName("usagePercentage")]
        public double UsagePercentage { get; set; }
        
        [JsonPropertyName("totalProcessorTimeSeconds")]
        public double TotalProcessorTimeSeconds { get; set; }
        
        [JsonPropertyName("userProcessorTimeSeconds")]
        public double UserProcessorTimeSeconds { get; set; }
        
        [JsonPropertyName("privilegedProcessorTimeSeconds")]
        public double PrivilegedProcessorTimeSeconds { get; set; }
        
        [JsonPropertyName("processorCount")]
        public int ProcessorCount { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class ProcessInfo
    {
        [JsonPropertyName("processId")]
        public int ProcessId { get; set; }
        
        [JsonPropertyName("processName")]
        public string ProcessName { get; set; } = string.Empty;
        
        [JsonPropertyName("threadCount")]
        public int ThreadCount { get; set; }
        
        [JsonPropertyName("handleCount")]
        public int HandleCount { get; set; }
        
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }
        
        [JsonPropertyName("uptime")]
        public UptimeInfo Uptime { get; set; } = new();
    }

    public class UptimeInfo
    {
        [JsonPropertyName("days")]
        public int Days { get; set; }
        
        [JsonPropertyName("hours")]
        public int Hours { get; set; }
        
        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }
        
        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }
        
        [JsonPropertyName("totalMinutes")]
        public double TotalMinutes { get; set; }
    }

    public class AllMetricsResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;
        
        [JsonPropertyName("summary")]
        public MetricsSummary Summary { get; set; } = new();
        
        [JsonPropertyName("memory")]
        public MemoryDetails Memory { get; set; } = new();
        
        [JsonPropertyName("cpu")]
        public CpuDetails Cpu { get; set; } = new();
        
        [JsonPropertyName("process")]
        public ProcessDetails Process { get; set; } = new();
        
        [JsonPropertyName("environment")]
        public EnvironmentDetails Environment { get; set; } = new();
    }

    public class MetricsSummary
    {
        [JsonPropertyName("memoryUsageMB")]
        public double MemoryUsageMB { get; set; }
        
        [JsonPropertyName("memoryLimitMB")]
        public double MemoryLimitMB { get; set; }
        
        [JsonPropertyName("memoryUsagePercentage")]
        public double MemoryUsagePercentage { get; set; }
        
        [JsonPropertyName("memoryStatus")]
        public string MemoryStatus { get; set; } = string.Empty;
        
        [JsonPropertyName("cpuUsagePercentage")]
        public double CpuUsagePercentage { get; set; }
        
        [JsonPropertyName("cpuStatus")]
        public string CpuStatus { get; set; } = string.Empty;
        
        [JsonPropertyName("threadCount")]
        public int ThreadCount { get; set; }
        
        [JsonPropertyName("uptimeMinutes")]
        public double UptimeMinutes { get; set; }
    }

    public class MemoryDetails
    {
        [JsonPropertyName("workingSetMB")]
        public double WorkingSetMB { get; set; }
        
        [JsonPropertyName("privateMemoryMB")]
        public double PrivateMemoryMB { get; set; }
        
        [JsonPropertyName("gcAllocatedMB")]
        public double GcAllocatedMB { get; set; }
        
        [JsonPropertyName("gcGen0Collections")]
        public int GcGen0Collections { get; set; }
        
        [JsonPropertyName("gcGen1Collections")]
        public int GcGen1Collections { get; set; }
        
        [JsonPropertyName("gcGen2Collections")]
        public int GcGen2Collections { get; set; }
    }

    public class CpuDetails
    {
        [JsonPropertyName("usagePercentage")]
        public double UsagePercentage { get; set; }
        
        [JsonPropertyName("totalProcessorTimeSeconds")]
        public double TotalProcessorTimeSeconds { get; set; }
        
        [JsonPropertyName("processorCount")]
        public int ProcessorCount { get; set; }
    }

    public class ProcessDetails
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("threads")]
        public int Threads { get; set; }
        
        [JsonPropertyName("handles")]
        public int Handles { get; set; }
        
        [JsonPropertyName("startTimeUtc")]
        public DateTime StartTimeUtc { get; set; }
    }

    public class EnvironmentDetails
    {
        [JsonPropertyName("machineName")]
        public string MachineName { get; set; } = string.Empty;
        
        [JsonPropertyName("osDescription")]
        public string OsDescription { get; set; } = string.Empty;
        
        [JsonPropertyName("dotNetVersion")]
        public string DotNetVersion { get; set; } = string.Empty;
        
        [JsonPropertyName("processorCount")]
        public int ProcessorCount { get; set; }
        
        [JsonPropertyName("is64Bit")]
        public bool Is64Bit { get; set; }
    }

    public class QuickStatusResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("memoryMB")]
        public double MemoryMB { get; set; }
        
        [JsonPropertyName("memoryPercent")]
        public double MemoryPercent { get; set; }
        
        [JsonPropertyName("threads")]
        public int Threads { get; set; }
        
        [JsonPropertyName("uptimeMinutes")]
        public int UptimeMinutes { get; set; }
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class GcResultResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonPropertyName("before")]
        public MemorySnapshot Before { get; set; } = new();
        
        [JsonPropertyName("after")]
        public MemorySnapshot After { get; set; } = new();
        
        [JsonPropertyName("freed")]
        public MemorySnapshot Freed { get; set; } = new();
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    public class MemorySnapshot
    {
        [JsonPropertyName("workingSetMB")]
        public double WorkingSetMB { get; set; }
        
        [JsonPropertyName("gcAllocatedMB")]
        public double GcAllocatedMB { get; set; }
    }

    #endregion
}
