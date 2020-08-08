USE [QuickFlowDb]
GO
/****** Object:  StoredProcedure [dbo].[sp_EnviarLeitura]    Script Date: 06/08/2020 15:09:40 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[sp_EnviarLeitura] 
	@ptl_id nvarchar(003),
	@mensagem nvarchar(MAX)

AS
BEGIN TRANSACTION
PRINT N'DEBUG|LC|Definindo parametros...';
--Parametros de leitura:

--Mensagem de entrada
DECLARE @message as varchar(max); 

SET @message = @mensagem;

-- Separador de mensagem
DECLARE @msgSeparator as varchar(1);  
SET @msgSeparator = '|';
--Tipo da mensagem
DECLARE @msgType as varchar(5); 
--Dispositivo que a enviou
DECLARE @msgDeviceId varchar(64);
--Leitura
DECLARE @msgRead as varchar(128);
-- Separador de mensagem
DECLARE @tamanhoCodigoLogin as int;  
SET @tamanhoCodigoLogin = 7;

DECLARE @estacaoId as uniqueidentifier;
DECLARE @doctoId as uniqueidentifier;
DECLARE @doctoAlocacaoId as uniqueidentifier;
DECLARE @usuarioLogadoId as uniqueidentifier;
DECLARE @usuarioLogado as varchar(128);
DECLARE @usuarioLogandoId as uniqueidentifier;
DECLARE @usuarioLogando as varchar(128);
DECLARE @AlocacoesRestantes as int;
DECLARE @masterDisplayId as varchar(128);

PRINT N'DEBUG|LC|Parametros definidos!';

PRINT N'INFO||Parametros:';
PRINT N'INFO||@msgSeparator:';
PRINT @msgSeparator;
PRINT N'INFO||@tamanhoCodigoLogin:';
PRINT @tamanhoCodigoLogin;


PRINT N'DEBUG||Criando tabela temporária #TT_Params...';
-- Criando uma tabela temporaria para guardar os parametros
create table #TT_Params
					( 
						ParamNumber int,
						ParamValue Varchar(64),
					);
PRINT N'DEBUG||Tabela temporária #TT_Params criada!';


PRINT N'DEBUG||Inserindo os parametros na tabela temporária #TT_Params...';
--Extraindo os parametros utilizando o separador 
INSERT INTO #TT_Params (ParamNumber, ParamValue) 
SELECT 
	ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS rownum,
	[value]
FROM 
	STRING_SPLIT(@message, @msgSeparator)
WHERE 
	[value] <> '';
PRINT N'DEBUG||Parametros inseridos na tabela temporária #TT_Params!';


PRINT N'DEBUG||Atribuindo as variáveis de parametros...';
--TODO: Possivel refactor 
SELECT TOP 1 @msgType = ParamValue		FROM #TT_Params WHERE ParamNumber = 2;
SELECT TOP 1 @msgDeviceId = ParamValue	FROM #TT_Params WHERE ParamNumber = 3;
SELECT TOP 1 @msgRead = ParamValue		FROM #TT_Params WHERE ParamNumber = 4;

PRINT N'DEBUG||Variáveis de parametros atrbuídas';


PRINT N'INFO||Parametros:';
PRINT N'INFO||@msgType:';
PRINT @msgType;
PRINT N'INFO||@msgDeviceId:';
PRINT @msgDeviceId;
PRINT N'INFO||@msgRead:';
PRINT @msgRead;
PRINT N'INFO||@ptl_id:';
PRINT @ptl_id;
PRINT N'INFO||@ptl_id:';
PRINT @ptl_id;


PRINT N'DEBUG||Removendo a tabela temporária #TT_Params...';
--Removendo a tabela temporária
DROP TABLE #TT_Params;
PRINT N'DEBUG||Tabela temporária #TT_Params removida!';

PRINT N'DEBUG||Obtendo a estação...';
	SELECT 
		@estacaoId =  es.Id,
		@masterDisplayId = es.CodigodisplayMestre
	FROM 
		Estacao es 						
	WHERE 
		es.CodigoLeitor = @msgDeviceId
		AND es.Ativo = 1;
PRINT N'DEBUG||Estação Obtida!';
				
PRINT N'INFO||@estacaoId:';
PRINT @estacaoId;
PRINT N'INFO||@masterDisplayId:';
PRINT @masterDisplayId;


PRINT N'DEBUG||Obtendo último usuário aberto para a estação...';
	Select TOP 1 
		@usuarioLogado = c.Usuario,
		@usuarioLogadoId = c.Id
	FROM 
		LoginCartao lg
		INNER JOIN Cartao c on lg.CartaoId = c.Id
	WHERE 
		lg.DtSaida IS NULL
		AND lg.EstacaoId = @estacaoId
	ORDER BY 
		lg.DtEntrada desc
PRINT N'DEBUG||Último usuário aberto para a estação obtido!';
PRINT N'INFO||@usuarioLogado:';
PRINT @usuarioLogado;

--Se for Leitor 
IF @msgType  = 'LC' 
	BEGIN
	    PRINT N'DEBUG|LC|INICIO LC...';

		PRINT N'DEBUG|LC|Verificando se é a leitura de um cartão...';
		IF LEN(@msgRead) = @tamanhoCodigoLogin
			BEGIN
				PRINT N'DEBUG|CT|Verificado que É uma leitura de cartão!';
								
								
				PRINT N'DEBUG|CT|Obtendo usuário logando...';
				Select TOP 1 
					@usuarioLogandoId = c.Id,
					@usuarioLogando = c.Usuario
				FROM 
					Cartao c 
				WHERE 
					c.CodCartao = @msgRead
				

				IF @usuarioLogandoId IS NULL
				BEGIN
					PRINT N'DEBUG|CT|Nenhum usuário cadastrado para esse código!';	
					Insert into Comando (Id, QuantReq, DtInicio,Cor,Identificador, DisplayCode, Vinculo, MasterMessage, EstacaoId)
					VALUES 
						(NEWID(), 0, GETDATE(), 0, NEWID(), @masterDisplayId, 'LOGIN ERR', '1011', @estacaoId ) -- 1011:E_PTLMasterMessage.LoginErr
				END
				ELSE
				BEGIN
					PRINT N'DEBUG|CT|Usuário logando obtido!';	
					PRINT N'INFO|CT|@usuarioLogandoId:';
					PRINT @usuarioLogandoId;	
					PRINT N'INFO|CT|@usuarioLogando:';
					PRINT @usuarioLogando;
				
				
					
					PRINT N'DEBUG|CT|Removendo todos os comandos pendentes daquela estação...';
					UPDATE Comando
						SET 
							DtFim = GETDATE(),
							DtProcessamento = GETDATE()
					WHERE 
						EstacaoId = @estacaoId 
						AND DtFim IS NULL
						AND DtAtendimento IS NULL 
						AND DtProcessamento IS NULL
					PRINT N'DEBUG|CT|Removido todos os comandos pendentes daquela estação...';


					PRINT N'DEBUG|CT|Efetuando logout se for o mesmo cartão...';
					Update LoginCartao
					SET DtSaida = GETDATE()
					Where
						EstacaoId = @estacaoId
						AND CartaoId = @usuarioLogandoId
						AND DtSaida IS NULL
					
					IF @@ROWCOUNT = 0  
						BEGIN

							Update LoginCartao
							SET DtSaida = GETDATE()
							Where
								EstacaoId = @estacaoId
								AND DtSaida IS NULL

							IF @@ROWCOUNT <> 0  
							BEGIN
								PRINT N'DEBUG|CT|Logout efetuado de outro usuário aberto!';
							END

							INSERT INTO LoginCartao (Id, DtEntrada, EstacaoId, CartaoId)
							VALUES
								(NEWID(),GETDATE(),@estacaoId,@usuarioLogandoId);


							UPDATE EstacaoPedido
								SET
									DtFimAtendimento = GETDATE()
							WHERE
								EstacaoId = @estacaoId
								AND DtFimAtendimento IS NULL
	
							INSERT INTO Comando (Id, QuantReq, DtInicio,Cor,Identificador, DisplayCode, Vinculo, MasterMessage, EstacaoId)
							VALUES 
								(NEWID(), 0, GETDATE(), 1, NEWID(), @masterDisplayId, SUBSTRING(CONCAT(@usuarioLogando, ' OK'), 1, 12), '1010', @estacaoId ) -- 1010:E_PTLMasterMessage.LoginOk

							PRINT N'DEBUG|CT|Login Efetuado!';
						END
					ELSE  
						BEGIN
							PRINT N'DEBUG|CT|Logout efetuado desse usuário!';
						END					
				END
			END
		ELSE
			BEGIN
				PRINT N'DEBUG|LC|Efeturnado o atendimento com o usuário:';
				PRINT @usuarioLogado;

				IF @usuarioLogadoId IS NOT NULL
				BEGIN
				PRINT N'DEBUG|LC|Verificado que NÃO É uma leitura de cartão!';

				PRINT N'DEBUG|LC|Criando tabela temporária #TT_Itens...';
				-- Criando uma tabela temporaria para guardar os resultados
				create table #TT_Itens
							(
								Position nvarchar(16), 
								QtdeRestante int,
								EstacaoId uniqueidentifier,
								Docto Varchar(64),
								DoctoId uniqueidentifier,
								DoctoAlocacaoId uniqueidentifier,
							)					
				PRINT N'DEBUG|LC|Tabela temporária #TT_Itens criada!';
		
		
				PRINT N'DEBUG|LC|Inserindo os itens na tabela temporária #TT_Itens...';	
				--Encontrando os itens desse pedido daquele LC e que estão em uma estação ativa e não estão com status de : Finalizado (60), FinalizadoManualmente (61)
				--Se for complemento (Polux 50 ou 100), a posição é subistituida pelo padrão da estação
				INSERT INTO #TT_Itens (Position, QtdeRestante,EstacaoId, Docto, DoctoId, DoctoAlocacaoId)
					Select 
						CASE 
							WHEN it.Nome = 'Polux50'
								THEN es.CodigoDisplay50
							WHEN it.Nome = 'Polux100'
								THEN es.CodigoDisplay100
							ELSE
								ua.PosicaoPickToLight
						END as Position,
						da.QtdeAlocada - COALESCE(da.QtdeAtendida,0) as QtdeRestante,
						es.Id as EstacaoId,
						doc.Identificador,
						doc.Id as DoctoId,
						da.Id as DoctoAlocacaoId
					from 
						Docto doc
						INNER JOIN DoctoAlocacao da ON doc.Id = da.DoctoId
						INNER JOIN Item it ON da.ItemId = it.Id
						INNER JOIN UnidadeArmazenamento ua ON it.UnidadeArmazenamentoId = ua.Id
						INNER JOIN GrupoArmazenamento ga ON  ua.Id = ga.UnidadeArmazenamentoId
						INNER JOIN Estacao es ON  ga.EstacaoId = es.Id
					where 
						doc.BarCode = @msgRead
						AND es.CodigoLeitor = @msgDeviceId
						AND es.Ativo = 1
						AND NOT (doc.[status] = 60 OR doc.[status] = 61);
				PRINT N'DEBUG|LC|Itens inseridos na tabela temporária #TT_Itens!';
					
					
				PRINT N'DEBUG|LC|Obtendo os campos @estacaoId e @doctoId...';
				SELECT TOP 1 
					@estacaoId = EstacaoId,
					@doctoId = DoctoId
				FROM #TT_Itens;		
				PRINT N'DEBUG|LC|Campos @estacaoId e @doctoId obtidos';
		

				PRINT N'INFO|LC|@estacaoId:';
				PRINT @estacaoId;
				PRINT N'INFO|LC|@doctoId:';
				PRINT @doctoId;
		
		
				PRINT N'DEBUG|LC|Finalizando os comandos pendentes da estação...';
				--Finalizando todos os comandos pendentes daquela estacao
				UPDATE Comando 
				SET 
					DtProcessamento = GETDATE(),
					DtAtendimento = GETDATE()
				WHERE 
					DtAtendimento is null
					AND EstacaoId = @estacaoId;
				PRINT N'DEBUG|LC|Comandos pendentes da estação dinalizados!';

				PRINT N'DEBUG|LC|Verificando e fechando outros pedidos abertos para essa estação...';
					UPDATE EstacaoPedido
						SET DtFimAtendimento = GETDATE()
					WHERE 
						EstacaoId = @estacaoId
						AND DtFimAtendimento IS NULL
						AND NOT (DoctoId = @doctoId AND CartaoId = @usuarioLogadoId AND EstacaoId = @estacaoId)
				PRINT N'DEBUG|LC|Verificados e fechados outros pedidos abertos para essa estação!';

				PRINT N'DEBUG|LC|Verificando o relacionamento EstacaoPedido já existe ...';
				   IF NOT EXISTS (SELECT * 
									FROM EstacaoPedido 
									WHERE 
										DtFimAtendimento IS NULL
										AND DoctoId = @doctoId
										AND CartaoId = @usuarioLogadoId
										AND EstacaoId = @estacaoId)
				   BEGIN
						PRINT N'DEBUG|LC|Inserindo o relacionamento EstacaoPedido...';
						   INSERT INTO EstacaoPedido
							(Id, TenantId, DtInicioAtendimento, DtFimAtendimento, DoctoId, CartaoId, EstacaoId)
							VALUES
								(NEWID(), null, GETDATE(), null, @doctoId, @usuarioLogadoId, @estacaoId)
						PRINT N'DEBUG|LC|Relacionamento EstacaoPedido inserido';
				   END
				PRINT N'DEBUG|LC|Verificado se o relacionamento EstacaoPedido já existe!';
		

				PRINT N'DEBUG|LC|Inserindo novos comandos...';
				--Inserindo os comandos novos
				Insert into Comando (Id, QuantReq, DtInicio,Cor,Identificador, DisplayCode, Vinculo, MasterMessage, EstacaoId)
				Select NEWID(), QtdeRestante, GETDATE(), 0, DoctoAlocacaoId, Position, '', '1000', @estacaoId 
					FROM #TT_Itens
					WHERE QtdeRestante > 0;
				PRINT N'DEBUG|LC|Novos comandos inseridos!';
		
		
				PRINT N'DEBUG|LC|Atualizando status do Docto para atendendo...';
				 --Atualizando o status do docto para atendendo (20)
				 Update Docto 
				 SET [Status] = 20
				 WHERE Id = @doctoId
				PRINT N'DEBUG|LC|Status do Docto alterado para atendendo!';

		 
				PRINT N'DEBUG|LC|Removendo a tabela temporária #TT_Itens...';
				 --Removendo a tabela temporária
				 DROP TABLE #TT_Itens;
				PRINT N'DEBUG|LC|Tabela temporária #TT_Itens removida!';

			END

			
			END
		PRINT N'DEBUG|LC|FIM LC!';
	END
ELSE IF @msgType = 'AT'
BEGIN
	PRINT N'DEBUG|AT|INICIO AT...';
	
	PRINT N'DEBUG|AT|Finalizando atendimentos pendentes...';
	UPDATE Comando
	SET 
		QuantAtend = CAST(@msgRead AS DECIMAL(18,2)),
		DtAtendimento = GETDATE(),
		@doctoAlocacaoId = Identificador,
		@estacaoId = EstacaoId
	WHERE 
		DtAtendimento IS NULL
		AND DisplayCode = concat(@ptl_id, ':', @msgDeviceId)
	PRINT N'DEBUG|AT|Atendimentos pendentes finalizados!';


	PRINT N'INFO|AT|@estacaoId:';
	PRINT @estacaoId;
	PRINT N'INFO|AT|@doctoAlocacaoId:';
	PRINT @doctoAlocacaoId;
	

	PRINT N'DEBUG|AT|Obtendo último usuário aberto para a estação...';
	Select TOP 1 @usuarioLogado = c.Usuario
	FROM 
		LoginCartao lg
		INNER JOIN Cartao c on lg.CartaoId = c.Id
	WHERE 
		lg.DtSaida IS NULL
		AND lg.EstacaoId = @estacaoId
	ORDER BY 
		lg.DtEntrada desc
	PRINT N'DEBUG|AT|Último usuário aberto para a estação obtido!';
	PRINT N'INFO|AT|@usuarioLogado:';
	PRINT @usuarioLogado;

	
	PRINT N'DEBUG|AT|Atualizando a alocação para o valor atendido...';
	UPDATE DoctoAlocacao
	SET 
		QtdeAtendida = @msgRead,
		DtAtendimento = GETDATE(),
		UsuarioAtendimento = COALESCE(@usuarioLogado, 'N/A'),
		@doctoId = DoctoId
	WHERE 
		Id =  @doctoAlocacaoId;
	PRINT N'DEBUG|AT|Alocação atualizada para o valor atendido!';		
			
			
	PRINT N'DEBUG|AT|Verificando a quantidade de alocações restantes para finalizar o docto...';
	--Verificando se todos as alocações daquele docto foram atendidas, se foram marca como concluido
	SELECT
		@AlocacoesRestantes = Count(*) 
	FROM 
		DoctoAlocacao da
	WHERE 
		da.DoctoId = @doctoId
		AND DtAtendimento IS NULL
	PRINT N'DEBUG|AT|Quantidade de alocações restantes para finalizar o docto verificada!';


	PRINT N'INFO|AT|@AlocacoesRestantes:';
	PRINT @AlocacoesRestantes;


	IF @AlocacoesRestantes  = 0
		BEGIN
			PRINT N'DEBUG|AT|Finalizando Docto...';

			Update Docto
			SET 
				[Status] = 50, -- Pronto (50)
				DtFimAtendimento = GETDATE()
			WHERE Id = @doctoId;

			Update EstacaoPedido
				SET DtFimAtendimento = GETDATE()
			WHERE 
				@doctoId = @doctoId
				AND DtFimAtendimento IS NULL

			
			SELECT 
				@masterDisplayId = es.CodigodisplayMestre
			FROM 
				Estacao es 						
			WHERE 
				es.id = @estacaoId

			INSERT INTO Comando (Id, QuantReq, DtInicio,Cor,Identificador, DisplayCode, Vinculo, MasterMessage, EstacaoId)
			VALUES 
				(NEWID(), 0, GETDATE(), 1, NEWID(), @masterDisplayId, 'FIM', '1001', @estacaoId ) -- 1001:E_PTLMasterMessage.ConfirmValue

			PRINT N'DEBUG|AT|Docto Finalizado';
		END
	ELSE
		BEGIN
			PRINT N'DEBUG|AT|Docto ainda em atendimento.';
		END

	PRINT N'DEBUG|AT|FIM AT!';
END

COMMIT