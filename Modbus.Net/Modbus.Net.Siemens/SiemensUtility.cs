﻿using System;
using System.Threading.Tasks;

namespace Modbus.Net.Siemens
{
    public enum SiemensType
    {
        Ppi = 0,
        Mpi = 1,
        Tcp = 2
    };

    public enum SiemensMachineModel
    {
        S7_200 = 0,
        S7_200_Smart = 1,
        S7_300 = 2,
        S7_400 = 3,
        S7_1200 = 4,
        S7_1500 = 5
    };


    public class SiemensUtility : BaseUtility
    {
        private readonly byte _tdpuSize;
        private readonly ushort _taspSrc;
        private readonly ushort _tsapDst;
        private readonly ushort _maxCalling;
        private readonly ushort _maxCalled;
        private readonly ushort _maxPdu;

        public override bool GetLittleEndian => Wrapper[typeof (ReadRequestSiemensProtocal)].IsLittleEndian;
        public override bool SetLittleEndian => Wrapper[typeof (WriteRequestSiemensProtocal)].IsLittleEndian;

        protected string ConnectionStringIp
        {
            get
            {
                if (ConnectionString == null) return null;
                return ConnectionString.Contains(":") ? ConnectionString.Split(':')[0] : ConnectionString;
            }
        }

        protected int? ConnectionStringPort
        {
            get
            {
                if (ConnectionString == null) return null;
                if (!ConnectionString.Contains(":")) return null;
                var connectionStringSplit = ConnectionString.Split(':');
                try
                {
                    return connectionStringSplit.Length < 2 ? (int?)null : int.Parse(connectionStringSplit[1]);
                }
                catch
                {
                    return null;
                }
            }
        }

        private SiemensType _siemensType;

        public SiemensType ConnectionType
        {
            get
            {
                return _siemensType;
            }
            set
            {
                _siemensType = value;
                switch (_siemensType)
                {
                    case SiemensType.Ppi:
                    {
                        Wrapper = ConnectionString == null ? new SiemensPpiProtocal(BelongAddress, MasterAddress) : new SiemensPpiProtocal(ConnectionString, BelongAddress, MasterAddress);
                        break;
                    }
                    //case SiemensType.Mpi:
                    //    {
                    //        throw new NotImplementedException();
                    //    }
                    case SiemensType.Tcp:
                    {
                        Wrapper = ConnectionString == null ? new SiemensTcpProtocal(_tdpuSize, _taspSrc, _tsapDst, _maxCalling, _maxCalled, _maxPdu) : (ConnectionStringPort == null ? new SiemensTcpProtocal(_tdpuSize, _taspSrc, _tsapDst, _maxCalling, _maxCalled, _maxPdu, ConnectionString) : new SiemensTcpProtocal(_tdpuSize, _taspSrc, _tsapDst, _maxCalling, _maxCalled, _maxPdu, ConnectionStringIp, ConnectionStringPort.Value));
                        break;
                    }
                }
            }
        }

        public SiemensUtility(SiemensType connectionType, string connectionString, SiemensMachineModel model,
            byte belongAddress, byte masterAddress) : base(belongAddress, masterAddress)
        {
            ConnectionString = connectionString;
            switch (model)
            {
                case SiemensMachineModel.S7_200:
                {
                    _tdpuSize = 0x09;
                    _taspSrc = 0x1001;
                    _tsapDst = 0x1000;
                    _maxCalling = 0x0001;
                    _maxCalled = 0x0001;
                    _maxPdu = 0x03c0;
                    break;
                }
                case SiemensMachineModel.S7_300:
                case SiemensMachineModel.S7_400:
                {
                    _tdpuSize = 0x1a;
                    _taspSrc = 0x4b54;
                    _tsapDst = 0x0302;
                    _maxCalling = 0x0001;
                    _maxCalled = 0x0001;
                    _maxPdu = 0x00f0;
                    break;
                }
                case SiemensMachineModel.S7_1200:
                case SiemensMachineModel.S7_1500:
                {
                    _tdpuSize = 0x0a;
                    _taspSrc = 0x1011;
                    _tsapDst = 0x0301;
                    _maxCalling = 0x0003;
                    _maxCalled = 0x0003;
                    _maxPdu = 0x0100;
                    break;
                }
                case SiemensMachineModel.S7_200_Smart:
                {
                    _tdpuSize = 0x0a;
                    _taspSrc = 0x0101;
                    _tsapDst = 0x0101;
                    _maxCalling = 0x0001;
                    _maxCalled = 0x0001;
                    _maxPdu = 0x03c0;
                    break;
                }
                default:
                {
                    throw new NotImplementedException("没有相应的西门子类型");
                }
            }
            ConnectionType = connectionType;
            AddressTranslator = new AddressTranslatorSiemens();
        }

        public override void SetConnectionType(int connectionType)
        {
            ConnectionType = (SiemensType) connectionType;
        }

        public override async Task<byte[]> GetDatasAsync(string startAddress, int getByteCount)
        {
            try
            {
                var readRequestSiemensInputStruct = new ReadRequestSiemensInputStruct(BelongAddress, MasterAddress, 0xd3c7, SiemensTypeCode.Byte, startAddress, (ushort)getByteCount, AddressTranslator);
                var readRequestSiemensOutputStruct =
                    await
                        Wrapper.SendReceiveAsync(Wrapper[typeof (ReadRequestSiemensProtocal)],
                            readRequestSiemensInputStruct) as ReadRequestSiemensOutputStruct;
                return readRequestSiemensOutputStruct?.GetValue;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override async Task<bool> SetDatasAsync(string startAddress, object[] setContents)
        {
            try
            {
                var writeRequestSiemensInputStruct = new WriteRequestSiemensInputStruct(BelongAddress, MasterAddress, 0xd3c8, startAddress, setContents, AddressTranslator);
                var writeRequestSiemensOutputStruct =
                    await
                        Wrapper.SendReceiveAsync(Wrapper[typeof (WriteRequestSiemensProtocal)],
                            writeRequestSiemensInputStruct) as WriteRequestSiemensOutputStruct;
                return writeRequestSiemensOutputStruct?.AccessResult == SiemensAccessResult.NoError;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /*
        public override DateTime GetTime(byte belongAddress)
        {
            throw new NotImplementedException();
        }

        public override bool SetTime(byte belongAddress, DateTime setTime)
        {
            throw new NotImplementedException();
        }
        */
    }
}
