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
        WallFinish = 1008,
        EfetuarLogin = 1009,
        LoginOk = 1010,
        LoginErr = 1011,
        PedidoScanOk = 1012,
        PedidoConcluido = 1013,
        FinalizadoParcial = 1014,
    }

    public static class MasterMessageExtensions
    {
        /// <summary>
        /// Obtem a mensagem a ser exibida e a cor
        /// </summary>
        /// <param name="PTLMessage">Helper this</param>
        /// <param name="Pedido">Identificador do pedido a ser exibido</param>
        /// <param name="TipoCaixa">Tipo da caixa a ser exibido</param>
        /// <param name="MaxDigitosDisplay">Quantidade de digitos do display, default = 12</param>
        /// <param name="MaxDigitosPedido">Quantidade de digitos do pedido a ser exibido, default = 8</param>
        /// <param name="MaxDigitosTipoCaixa">Quantidade de digitos do tipo da caixa a ser exibido, default = 3</param>
        /// <returns></returns>
        public static (string message, E_DisplayColor color) GetMessageAndColor(this E_PTLMasterMessage PTLMessage, string Vinculo = "")
        {
            var defaultColor = E_DisplayColor.Vermelho;
            
            return PTLMessage switch
            {
                E_PTLMasterMessage.None => (string.Empty, defaultColor),
                E_PTLMasterMessage.ErrorValue => ("Err", defaultColor),
                E_PTLMasterMessage.ConfirmValue => ("Fin", E_DisplayColor.Verde),
                E_PTLMasterMessage.ItemHasNoOrder => ("SM PED", defaultColor),
                E_PTLMasterMessage.ItemOk => ("IT OK", E_DisplayColor.Verde),
                E_PTLMasterMessage.ToteInvalid => ("TT INV", defaultColor),
                E_PTLMasterMessage.ToteOk => ("TT OK", E_DisplayColor.Verde),
                E_PTLMasterMessage.WallFinish => ("WL FIM", E_DisplayColor.Verde),
                E_PTLMasterMessage.WallOcuped => ("WL OCP", defaultColor),
                E_PTLMasterMessage.EfetuarLogin => ("LOGAR", defaultColor),
                E_PTLMasterMessage.LoginOk => ("LOGIN OK", defaultColor),
                E_PTLMasterMessage.LoginErr => ("LOGIN Err", defaultColor),
                E_PTLMasterMessage.PedidoConcluido => ("PED CONCL", defaultColor),
                E_PTLMasterMessage.FinalizadoParcial => ("PARCIAL", defaultColor),
                E_PTLMasterMessage.PedidoScanOk => (Vinculo, E_DisplayColor.Verde),
                _ => (string.Empty, defaultColor),
            };
        }

        public static E_PtlMessageType MessageType(this E_PTLMasterMessage PTLMessage) => ((PTLMessage == E_PTLMasterMessage.None) ? E_PtlMessageType.PickingMessage : E_PtlMessageType.MasterMessage);
    }
}
