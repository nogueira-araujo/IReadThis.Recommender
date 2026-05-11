USE IReadThis;
GO

-- Criação da Tabela Sidecar para Inteligência
CREATE TABLE Sidecar_BookEmbeddings (
    BookID INT PRIMARY KEY,
    -- Vetor de 768 dimensões (compatível com modelos como BERT/TensorFlow)
    Embedding VECTOR(768) NOT NULL, 
    LastUpdated DATETIME DEFAULT GETDATE(),
    
    -- Chave Estrangeira garantindo integridade com o legado
    CONSTRAINT FK_BookEmbeddings_Books FOREIGN KEY (BookID) 
        REFERENCES Books(BookID) ON DELETE CASCADE
);
GO

CREATE TABLE Sidecar_ModelCheckpoints (
    CheckpointID INT IDENTITY(1,1) PRIMARY KEY,
    VersionName VARCHAR(50) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    ModelZipData VARBINARY(MAX) NOT NULL
);