USE IReadThis;
GO

-- Criação da Tabela Sidecar para Inteligência
CREATE TABLE BookEmbeddings (
    BookID INT PRIMARY KEY,
    -- Vetor de 768 dimensões (compatível com modelos como BERT/TensorFlow)
    Embedding VECTOR(768) NOT NULL, 
    LastUpdated DATETIME DEFAULT GETDATE(),
    
    -- Chave Estrangeira garantindo integridade com o legado
    CONSTRAINT FK_BookEmbeddings_Books FOREIGN KEY (BookID) 
        REFERENCES Books(BookID) ON DELETE CASCADE
);
GO