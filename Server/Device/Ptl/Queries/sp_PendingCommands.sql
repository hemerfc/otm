USE [QuickFlowDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_PendingCommands]    Script Date: 05/08/2020 17:11:03 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER    PROCEDURE [dbo].[sp_PendingCommands]
   @p1 nvarchar(10), 
   @p2 nvarchar(250) OUTPUT
AS
BEGIN

declare @resultFinal nvarchar(250)
set @resultFinal = ''
/*
Cores PTL

Vermelho = 0x00,
Verde = 0x01,
Laranja = 0x02,
Azul = 0x03,
Rosa = 0x04,
Aqua = 0x05,
Off = 0x06
*/

PRINT N'DEBUG||Criando a tabela temporária de comandos...';
CREATE TABLE #tt_Comandos(
	[Id] [uniqueidentifier] NOT NULL,
	[TenantId] [int] NULL,
	[QuantReq] [decimal](18, 2) NOT NULL,
	[QuantAtend] [decimal](18, 2) NULL,
	[DtInicio] [datetime2](7) NOT NULL,
	[DtAtendimento] [datetime2](7) NULL,
	[DtProcessamento] [datetime2](7) NULL,
	[DtFim] [datetime2](7) NULL,
	[Cor] [nvarchar](1) NOT NULL,
	[Identificador] [uniqueidentifier] NOT NULL,
	[DisplayCode] [nvarchar](16) NOT NULL,
	[Vinculo] [nvarchar](max) NOT NULL,
	[EstacaoId] [uniqueidentifier] NOT NULL,
	[MasterMessage] [int] NOT NULL);
PRINT N'DEBUG||Tabela temporária de comandos criada...';
DELETE FROM #tt_Comandos;

PRINT N'DEBUG||Obtendo estações não logadas...';
    INSERT INTO #tt_Comandos			
	SELECT NEWID() AS [Id], 
			null AS[TenantId], 
			0.0 AS [QuantReq], 
			null AS [QuantAtend], 
			GETDATE() AS [DtInicio], 
	       null AS [DtAtendimento],
		   null AS [DtProcessamento], 
		   null AS [DtFim], 
		   0 AS [Cor], 
		   NEWID() AS [Identificador],
		   es.CodigoDisplayMestre AS [DisplayCode], 
		   'LOGAR' AS [Vinculo],
		   es.Id AS [EstacaoId], 
		   1009 AS [MasterMessage]  --1009 E_PTLMasterMessage.EfetuarLogin	
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


PRINT N'DEBUG||Obtendo estações logadas mas sem pedido...';
INSERT INTO #tt_Comandos			
	SELECT NEWID() AS [Id], 
			null AS[TenantId], 
			0.0 AS [QuantReq], 
			null AS [QuantAtend], 
			GETDATE() AS [DtInicio], 
	       null AS [DtAtendimento],
		   null AS [DtProcessamento], 
		   null AS [DtFim], 
		   1 AS [Cor], 
		   NEWID() AS [Identificador],
		   es.CodigoDisplayMestre AS [DisplayCode], 
		   'LOGIN OK' AS [Vinculo],
		   es.Id AS [EstacaoId], 
		   1010 AS [MasterMessage]  --1010 E_PTLMasterMessage.LoginOk
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



PRINT N'DEBUG||Obtendo estações logadas e com pedido...';


	INSERT INTO #tt_Comandos			
	SELECT NEWID() AS [Id], 
			null AS[TenantId], 
			0.0 AS [QuantReq], 
			null AS [QuantAtend], 
			GETDATE() AS [DtInicio], 
	       null AS [DtAtendimento],
		   null AS [DtProcessamento], 
		   null AS [DtFim], 
		   1 AS [Cor], 
		   NEWID() AS [Identificador],
		   es.CodigoDisplayMestre AS [DisplayCode], 
		   CONCAT(CAST(RIGHT(doc.Identificador, 8) AS CHAR(8)),' ', CAST(RIGHT(emb.Nome, 3) AS CHAR(3))) AS [Vinculo],
		   es.Id AS [EstacaoId], 
		   1012 AS [MasterMessage]  --1012 E_PTLMasterMessage.PedidoScanOk
	FROM  
		Estacao es
		INNER JOIN EstacaoPedido ep ON ep.EstacaoId = es.Id
		INNER JOIN Docto doc on ep.DoctoId = doc.Id 
		INNER JOIN Embalagem emb on doc.EmbalagemSugeridaId = emb.Id 
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



PRINT N'DEBUG||Inserindo somente os comandos que não estão no banco...';
INSERT INTO [dbo].[Comando] 
		([Id], [TenantId], [QuantReq], [QuantAtend], [DtInicio], [DtAtendimento], [DtProcessamento], 
		[DtFim], [Cor], [Identificador], [DisplayCode], [Vinculo], [EstacaoId], [MasterMessage])
    SELECT es.[Id], es.[TenantId], es.[QuantReq], es.[QuantAtend], es.[DtInicio], es.[DtAtendimento], es.[DtProcessamento], 
		es.[DtFim], es.[Cor], es.[Identificador], es.[DisplayCode], es.[Vinculo], es.[EstacaoId], es.[MasterMessage]
	FROM #tt_Comandos es
	WHERE
		(es.DtAtendimento IS NULL OR es.DtFim IS NULL)
		AND NOT EXISTS(select * 
						from Comando c2 
						where c2.DisplayCode = es.DisplayCode
							AND c2.Cor = es.Cor
							AND c2.QuantReq = es.QuantReq
							AND c2.MasterMessage = es.MasterMessage
							AND c2.DtAtendimento IS NULL 
							AND c2.DtProcessamento IS NULL
							AND c2.DtFim IS NULL);
							
DROP TABLE #tt_Comandos;
PRINT N'DEBUG||Inseridos no banco somente os comandos que ainda não estavam...';


PRINT N'DEBUG||Obtendo atendimentos em aberto...';
	Select 
		@resultFinal =
		CASE
			WHEN @resultFinal != '' 
			THEN CONCAT(@resultFinal,';',DisplayCode,'|',Cor,'|',
			CASE WHEN vinculo != '' THEN Vinculo ELSE Format(QuantReq, 'F0') END,
			'|',MasterMessage,'|',Id)
			ELSE CONCAT(DisplayCode,'|',Cor,'|',
			CASE WHEN vinculo != '' THEN Vinculo ELSE Format(QuantReq, 'F0') END,
			'|',MasterMessage,'|',Id)
		END
	from comando
	Where DtAtendimento IS NULL and DtProcessamento is null;
PRINT N'DEBUG||Atendimentos em aberto obtidos...';
PRINT N'DEBUG||@resultFinal:';
PRINT @resultFinal;

SET @p2 = @resultFinal;
END

Select * from Comando
