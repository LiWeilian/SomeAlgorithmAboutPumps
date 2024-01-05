using Newtonsoft.Json;

namespace PumpsSchedule
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /*
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
            */

            //加载示例数据
            if (!File.Exists(".\\示例数据.json"))
            {
                _logger.LogWarning("未找到示例数据文件");
                return;
            }
            string sampleData = File.ReadAllText(".\\示例数据.json");
            PumpSchedulingParams? schedulingParams = JsonConvert.DeserializeObject<PumpSchedulingParams>(sampleData);
            if (schedulingParams == null)
            {
                _logger.LogWarning("加载示例数据失败");
                return;
            }

            try
            {
                _logger.LogInformation("开始运行泵组优化调度...");
                DateTime startTime = DateTime.Now;
                //分时段调度记录
                List<PumpSchedulingPlan> plans = PumpScheduling.Run(schedulingParams);

                string output = JsonConvert.SerializeObject(plans);
                string outputDir = $"{AppDomain.CurrentDomain.BaseDirectory}Output";
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                string outputFile = $"{outputDir}\\OutputPlans_{DateTime.Now.ToString("yyyyMMddHHmmss")}.json";
                File.WriteAllText(outputFile, output);

                double timeSpan = (DateTime.Now - startTime).TotalSeconds;
                _logger.LogInformation($"运行泵组优化调度完毕，耗时[{timeSpan.ToString("0.#")}]秒，结果已输出到Output目录，运算过程文件在Log目录");
            }
            catch (Exception ex)
            {
                _logger.LogError($"运行水泵优化调度发生错误：{ex.Message}");
            }
        }
    }
}