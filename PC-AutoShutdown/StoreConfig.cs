using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace PC_AutoShutdown {
    public class StoreConfig {

        public StoreConfigEntry[] Stores { get; private set; }

        public StoreConfig(string _xmlPath = "StoreConfig.xml") {
            //Create config file from embedded resource if it's not found
            if (!File.Exists(_xmlPath)) {
                Stream fallback = Assembly.GetExecutingAssembly().GetManifestResourceStream("PC_AutoShutdown.StoreConfigFallback.xml");
                using (FileStream fs = File.Create(_xmlPath)) {
                    fallback.Seek(0, SeekOrigin.Begin);
                    fallback.CopyTo(fs);
                    fs.Close();
                }
                fallback.Close();
            }

            LoadConfig(_xmlPath);
        }

        private void LoadConfig(string _xmlPath) {
            XmlReaderSettings settings = new XmlReaderSettings {
                IgnoreComments = true
            };
            XmlReader reader = XmlReader.Create(_xmlPath, settings);
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            XmlNode node = doc.SelectSingleNode("Stores");
            XmlNodeList storeNodes = node.SelectNodes("Store");

            Stores = new StoreConfigEntry[storeNodes.Count];
            for(int i = 0; i < storeNodes.Count; i++) {
                string storeName = storeNodes[i].Attributes.GetNamedItem("name").Value;

                XmlNodeList shutdownTimes = storeNodes[i].SelectSingleNode("AutoShutdown").ChildNodes;

                if(shutdownTimes.Count != 7) {
                    throw new StoreConfigException($"Every store needs 7 shutdown times defined (one for every day of the week). Found: {shutdownTimes.Count} in Store {storeName}");
                }

                TimeSpan[] times = new TimeSpan[shutdownTimes.Count];

                //0 = monday, 1 = tuesday, 2 = wednesday, ...
                for(int j = 0; j < shutdownTimes.Count; j++) {
                    XmlNode timeNode = shutdownTimes[j];
                    string time = timeNode.InnerText;
                    string[] split = time.Split(':');

                    if (time.Length != 5 || split.Length != 2 || !int.TryParse(split[0], out int hour) || !int.TryParse(split[1], out int minute)) {
                        throw new StoreConfigException($"Time format wrong in StoreConfig: {storeName}, Time: {time} ({timeNode.Name}) - Expected format: 00:00");
                    }

                    times[j] = new TimeSpan(hour, minute, 0);
                }

                Stores[i] = new StoreConfigEntry(storeName, times);
            }
        }

    }

    public class StoreConfigEntry {

        public string Name { get; private set; }
        public TimeSpan[] ShutdownTimes { get; private set; }

        public StoreConfigEntry(string _name, TimeSpan[] _shutdownTimes) {
            Name = _name;
            ShutdownTimes = _shutdownTimes;
        }

    }

    public class StoreConfigException : Exception {
        public StoreConfigException() { }

        public StoreConfigException(string _message) : base(_message) {

        }

    }
}
