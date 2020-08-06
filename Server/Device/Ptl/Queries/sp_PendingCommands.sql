USE [QuickFlowDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_PendingCommands]    Script Date: 05/08/2020 11:31:41 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER    PROCEDURE [dbo].[sp_PendingCommands]
   @p1 nvarchar(10), 
   @p2 nvarchar(250) OUTPUT
AS
BEGIN
--SET @p2 = CONCAT(@p1,':002|1|00000000001|1000;001:003|1||1004');
declare @resultAtendimentos nvarchar(250)
declare @resultNaoLogados nvarchar(250)
declare @resultLogadaSemPedido nvarchar(250)
declare @resultLogadaComPedido nvarchar(250)
declare @resultFinal nvarchar(250)

set @resultAtendimentos = ''
set @resultNaoLogados = ''
set @resultLogadaSemPedido = ''
set @resultLogadaComPedido = ''
set @resultFinal = ''

PRINT N'DEBUG||Obtendo atendimentos em aberto...';
	Select 
		@resultAtendimentos =
		CASE
			WHEN @resultAtendimentos != '' 
			THEN CONCAT(@resultAtendimentos,';',DisplayCode,'|1|',Format(QuantReq, 'F0'),'|',MasterMessage)
			ELSE CONCAT(DisplayCode,'|1|',Format(QuantReq, 'F0'),'|',MasterMessage)
		END
	from comando
	Where DtAtendimento IS NULL;
PRINT N'DEBUG||Atendimentos em aberto obtidos...';
PRINT N'DEBUG||@resultAtendimentos:';
PRINT @resultAtendimentos;

PRINT N'DEBUG||Obtendo estações não logadas...';
	Select 
		@resultNaoLogados =
		CASE
			WHEN @resultNaoLogados != '' 
			THEN CONCAT(@resultNaoLogados,';',CodigoDisplayMestre,'|1|0|1009') --1009 E_PTLMasterMessage.EfetuarLogin
			ELSE CONCAT(CodigoDisplayMestre,'|1|0|1009')
		END
	FROM 
		Estacao es
	WHERE
		es.Ativo = 1
		AND es.Id NOT IN
			(Select esAbertas.Id	
			 FROM 
				Estacao esAbertas
				LEFT JOIN LoginCartao lg on esAbertas.Id = lg.EstacaoId
			WHERE
				esAbertas.Ativo = 1
				AND lg.DtEntrada IS NOT NULL
				AND lg.DtSaida IS NULL
			);		
PRINT N'DEBUG||Estações não logadas obtidas obtidos...';
PRINT N'DEBUG||@resultNaoLogados:';
PRINT @resultNaoLogados;

PRINT N'DEBUG||Obtendo estações logadas mas sem pedido...';
	Select 
		@resultLogadaSemPedido =
		CASE
			WHEN @resultLogadaSemPedido != '' 
			THEN CONCAT(@resultLogadaSemPedido,';',CodigoDisplayMestre,'|1|0|1010') --1010 E_PTLMasterMessage.LoginOk
			ELSE CONCAT(CodigoDisplayMestre,'|1|0|1010')
		END
	FROM 
		Estacao es
		LEFT JOIN EstacaoPedido ep ON ep.EstacaoId = es.Id
	WHERE
		ep.Id IS NULL
		AND ep.DtFimAtendimento IS NULL 
		AND es.Ativo = 1
		AND es.Id IN
			(Select esAbertas.Id	
			 FROM 
				Estacao esAbertas
				LEFT JOIN LoginCartao lg on esAbertas.Id = lg.EstacaoId
			WHERE
				esAbertas.Ativo = 1
				AND lg.DtEntrada IS NOT NULL
				AND lg.DtSaida IS NULL
			);		
PRINT N'DEBUG||Estações estações logadas mas sem pedido obtidas...';
PRINT N'DEBUG||@resultLogadaSemPedido:';
PRINT @resultLogadaSemPedido;

PRINT N'DEBUG||Obtendo estações logadas e com pedido...';
	Select 
		@resultLogadaComPedido =
		CASE
			WHEN @resultLogadaComPedido != '' 
			THEN CONCAT(@resultLogadaComPedido,';',CodigoDisplayMestre,'|1|0|1012') --1012 E_PTLMasterMessage.PedidoScanOk
			ELSE CONCAT(CodigoDisplayMestre,'|1|0|1012')
		END
	FROM 
		Estacao es
		INNER JOIN EstacaoPedido ep ON ep.EstacaoId = es.Id
	WHERE
		ep.Id IS NOT NULL
		AND ep.DtInicioAtendimento IS NOT NULL 
		AND ep.DtFimAtendimento IS NULL 
		AND es.Ativo = 1
		AND es.Id IN
			(Select esAbertas.Id	
			 FROM 
				Estacao esAbertas
				LEFT JOIN LoginCartao lg on esAbertas.Id = lg.EstacaoId
			WHERE
				esAbertas.Ativo = 1
				AND lg.DtEntrada IS NOT NULL
				AND lg.DtSaida IS NULL
			);		
PRINT N'DEBUG||Estações estações logadas e com pedido obtidas...';
PRINT N'DEBUG||@resultLogadaComPedido:';
PRINT @resultLogadaComPedido;

PRINT N'DEBUG||Concatenando resultado final...';
		IF @resultAtendimentos != ''
		BEGIN
			IF @resultFinal != ''
			BEGIN
				SET @resultFinal = CONCAT(@resultFinal,';');
			END

			SET @resultFinal = CONCAT(@resultFinal, @resultAtendimentos);
		END

		IF @resultNaoLogados != ''
		BEGIN
			IF @resultFinal != ''
			BEGIN
				SET @resultFinal = CONCAT(@resultFinal,';');
			END

			SET @resultFinal = CONCAT(@resultFinal,@resultNaoLogados);
		END

		IF @resultLogadaSemPedido != ''
		BEGIN
			IF @resultFinal != ''
			BEGIN
				SET @resultFinal = CONCAT(@resultFinal,';');
			END

			SET @resultFinal = CONCAT(@resultFinal, @resultLogadaSemPedido);
		END
			
		IF @resultLogadaComPedido != ''
		BEGIN
			IF @resultFinal != ''
			BEGIN
				SET @resultFinal = CONCAT(@resultFinal,';');
			END

			SET @resultFinal = CONCAT(@resultFinal, @resultLogadaComPedido);
		END		
PRINT N'DEBUG||Resultado final concatenado...';
PRINT N'DEBUG||@resultFinal:';
PRINT @resultFinal;	


SET @p2 = @resultFinal;
END
