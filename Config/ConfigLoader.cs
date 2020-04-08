﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Banditlord
{
    class ConfigLoader
    {
        private static ConfigLoader _instance = null;
        public Config Config { get; private set; }

        public static ConfigLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigLoader();
                }

                return _instance;
            }
        }

        private ConfigLoader()
        {
            string path = Path.Combine(BasePath.Name, "Modules", "Banditlord", "ModuleData", "config.xml");
            Config = getConfig(path);
        }

        private Config getConfig(String filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                using (var reader = new StreamReader(filePath))
                {
                    return (Config)serializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                BanditModErrorHandler.HandleException(e);
                Config config = new Config();
                config.IsDebugMode = true;
                return config;
            }
        }
    }
}
