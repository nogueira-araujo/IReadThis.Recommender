-- Criar o banco de dados
CREATE DATABASE IReadThis;
GO

USE IReadThis;
GO

-- 1. Tabela de Perfis [cite: 3]
CREATE TABLE Profiles (
    ProfileID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL, -- [cite: 4]
    BirthYear INT NOT NULL,         -- [cite: 4]
    Sex CHAR(1) NOT NULL,           -- [cite: 4]
    CONSTRAINT CHK_Sex CHECK (Sex IN ('M', 'F')),
    CONSTRAINT CHK_BirthYear CHECK (BirthYear > 1900 AND BirthYear <= YEAR(GETDATE()))
);

-- 2. Tabela de Categorias [cite: 5]
CREATE TABLE Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    Description NVARCHAR(100) NOT NULL -- [cite: 6]
);

-- 3. Tabela de Livros [cite: 7]
CREATE TABLE Books (
    BookID INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,       -- 
    Author NVARCHAR(200) NOT NULL,      -- 
    Publisher NVARCHAR(150),            -- 
    ReleaseYear INT,                    -- 
    PageCount INT,                      -- 
    -- Coluna para suporte à recomendação via IA (768 dimensões como exemplo)
    BookEmbedding VECTOR(768) 
);

-- 4. Tabela de Junção: Livros x Categorias (Muitos-para-Muitos) 
CREATE TABLE BookCategories (
    BookID INT NOT NULL,
    CategoryID INT NOT NULL,
    PRIMARY KEY (BookID, CategoryID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID) ON DELETE CASCADE,
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE
);

-- 5. Tabela de Leituras e Avaliações 
CREATE TABLE Ratings (
    RatingID INT IDENTITY(1,1) PRIMARY KEY,
    ProfileID INT NOT NULL,             -- 
    BookID INT NOT NULL,                -- 
    Rating INT NOT NULL,                -- 
    ReadDate DATETIME DEFAULT GETDATE(),
    CONSTRAINT CHK_Rating CHECK (Rating BETWEEN 0 AND 4), -- Nota 0 a 4 
    FOREIGN KEY (ProfileID) REFERENCES Profiles(ProfileID),
    FOREIGN KEY (BookID) REFERENCES Books(BookID)
);
GO