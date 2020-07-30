namespace Otm.Server.Device.Ptl
{
    public enum E_PTLMasterMessage : int
    {
        None = 1000,
        ConfirmValue = 1001,
        ErrorValue = 1002,
        ItemOk = 1003,
        ItemHasNoOrder = 1004,
        ToteOk = 1005,
        ToteInvalid = 1006,
        WallOcuped = 1007,
        WallFinish = 1008
    }

    public static class MasterMessageExtensions
    {
        public static (string message, E_DisplayColor color) GetMessageAndColor(this E_PTLMasterMessage PTLMessage)
        {
            var defaultColor = E_DisplayColor.Vermelho;

            return PTLMessage switch
            {
                E_PTLMasterMessage.ErrorValue => ("Err", defaultColor),
                E_PTLMasterMessage.ConfirmValue => ("Fin", E_DisplayColor.Verde),
                E_PTLMasterMessage.ItemHasNoOrder => ("SM PED", defaultColor),
                E_PTLMasterMessage.ItemOk => ("IT OK", E_DisplayColor.Verde),
                E_PTLMasterMessage.ToteInvalid => ("TT INV", defaultColor),
                E_PTLMasterMessage.ToteOk => ("TT OK", E_DisplayColor.Verde),
                E_PTLMasterMessage.WallFinish => ("WL FIM", E_DisplayColor.Verde),
                E_PTLMasterMessage.WallOcuped => ("WL OCP", defaultColor),
                E_PTLMasterMessage.None => (string.Empty, defaultColor),
                _ => (string.Empty, defaultColor),
            };
        }

        public static E_PtlMessageType MessageType(this E_PTLMasterMessage PTLMessage) => ((PTLMessage == E_PTLMasterMessage.None) ? E_PtlMessageType.PickingMessage : E_PtlMessageType.MasterMessage);
    }
}
