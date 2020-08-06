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
        public static (string message, E_DisplayColor color) GetMessageAndColor(this E_PTLMasterMessage PTLMessage, string Pedido = "", string TipoCaixa = "", int MaxDigitosDisplay = 12, int MaxDigitosPedido = 8, int MaxDigitosTipoCaixa = 3)
        {
            var defaultColor = E_DisplayColor.Vermelho;

            #region Criando a mensagem de display do pedido
            //Obtendo o Pedido com o máximo de MaxDigitosPedido
            var pedidoStr = (Pedido.Length <= MaxDigitosPedido) ? Pedido : Pedido.Substring(Pedido.Length - MaxDigitosPedido);
            
            //Obtendo o TipoCaixa com o máximo de MaxDigitosTipoCaixa
            var tipoCaixaStr = (TipoCaixa.Length <= MaxDigitosTipoCaixa) ? TipoCaixa : TipoCaixa.Substring(TipoCaixa.Length - MaxDigitosTipoCaixa);

            //Inserindo o pedido com os espaços entre o pedido e o tipo da caixa
            var displayPedidoStr = pedidoStr.PadRight(MaxDigitosDisplay - tipoCaixaStr.Length, ' ');

            //Inserindo o tipo da caixa
            displayPedidoStr += tipoCaixaStr;

            #endregion Criando a mensagem de display do pedido


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
                E_PTLMasterMessage.PedidoScanOk => (displayPedidoStr, E_DisplayColor.Verde),
                _ => (string.Empty, defaultColor),
            };
        }

        public static E_PtlMessageType MessageType(this E_PTLMasterMessage PTLMessage) => ((PTLMessage == E_PTLMasterMessage.None) ? E_PtlMessageType.PickingMessage : E_PtlMessageType.MasterMessage);
    }
}
