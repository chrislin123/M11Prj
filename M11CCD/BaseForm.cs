using System.Windows.Forms;
using System.Configuration;
using M10.lib;
using NLog;
using System.Net;

namespace M11CCD
{
    public class BaseForm : Form
    {
        private string _ConnectionString;
        private string _ConnectionStringProcal;
        private DALDapper _dbDapper;
        private DALDapper _dbDapperProcal;
        public string ssql = string.Empty;
        public Logger logger;
        private StockHelper _stockhelper;

        public DALDapper dbDapper
        {
            get
            {
                if (_dbDapper == null)
                {
                    _dbDapper = new DALDapper(ConnectionString);
                }

                return _dbDapper;
            }
        }

        public string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_ConnectionString))
                {
                    _ConnectionString = ConfigurationManager.ConnectionStrings[ConfigurationManager.AppSettings["DBDefault"]].ConnectionString;
                }

                return _ConnectionString;
            }
        }

        public DALDapper dbDapperProcal
        {
            get
            {
                if (_dbDapperProcal == null)
                {
                    _dbDapperProcal = new DALDapper(ConnectionStringProcal);
                }

                return _dbDapperProcal;
            }
        }

        public string ConnectionStringProcal
        {
            get
            {
                if (string.IsNullOrEmpty(_ConnectionStringProcal))
                {
                    _ConnectionStringProcal = ConfigurationManager.ConnectionStrings[ConfigurationManager.AppSettings["DBProcal"]].ConnectionString;
                }

                return _ConnectionStringProcal;
            }
        }

        public StockHelper Stockhelper
        {
            get
            {
                if (_stockhelper == null)
                {
                    _stockhelper = new StockHelper();
                }
                return _stockhelper;
            }

        }

        public BaseForm()
        {

        }

        public void InitForm()
        {
            //_dbDapper = new DALDapper(ConnectionString);
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

    }
}
