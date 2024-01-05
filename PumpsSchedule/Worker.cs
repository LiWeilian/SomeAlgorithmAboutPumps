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

            //����ʾ������
            if (!File.Exists(".\\ʾ������.json"))
            {
                _logger.LogWarning("δ�ҵ�ʾ�������ļ�");
                return;
            }
            string sampleData = File.ReadAllText(".\\ʾ������.json");
            PumpSchedulingParams? schedulingParams = JsonConvert.DeserializeObject<PumpSchedulingParams>(sampleData);
            if (schedulingParams == null)
            {
                _logger.LogWarning("����ʾ������ʧ��");
                return;
            }

            try
            {
                _logger.LogInformation("��ʼ���б����Ż�����...");
                DateTime startTime = DateTime.Now;
                //��ʱ�ε��ȼ�¼
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
                _logger.LogInformation($"���б����Ż�������ϣ���ʱ[{timeSpan.ToString("0.#")}]�룬����������OutputĿ¼����������ļ���LogĿ¼");
            }
            catch (Exception ex)
            {
                _logger.LogError($"����ˮ���Ż����ȷ�������{ex.Message}");
            }
        }
    }
}