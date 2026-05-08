USE IReadThis;
GO

-- 1. LIMPEZA TOTAL COM RESET DE IDENTIDADE
-- Usamos TRUNCATE onde possível para resetar o IDENTITY de forma limpa e rápida
-- Nota: Ratings e BookCategories não podem sofrer TRUNCATE se houver FKs ativas, então usamos DELETE + CHECKIDENT
DELETE FROM Ratings;
DELETE FROM BookCategories;
DELETE FROM Books;
DELETE FROM Categories;
DELETE FROM Profiles;

-- Removendo a coluna de suporte a vetores para restaurar o estado legado
ALTER TABLE Books
DROP COLUMN IF EXISTS BookEmbedding;
GO

DBCC CHECKIDENT ('Profiles', RESEED, 0);
DBCC CHECKIDENT ('Categories', RESEED, 0);
DBCC CHECKIDENT ('Books', RESEED, 0);
DBCC CHECKIDENT ('Ratings', RESEED, 0);
GO -- O 'GO' é vital para que o SQL Server processe a limpeza antes da carga

-- 2. CARGA DE CATEGORIAS (SSoT)
INSERT INTO Categories (Description) VALUES 
('Ficção Científica'), ('Tecnologia'), ('Negócios'), ('Suspense'), ('Biografia'), 
('Filosofia'), ('História'), ('Psicologia'), ('Autoajuda'), ('Fantasia');
GO -- Garante que as 10 categorias existam antes do próximo passo

-- 3. PERFIS (30 Perfis)
INSERT INTO Profiles (FullName, BirthYear, Sex) VALUES 
('Marcelo Nogueira Araújo', 1978, 'M'), ('Ana Silva Santos', 1995, 'F'), ('Carlos Eduardo Lima', 1982, 'M'), ('Mariana Oliveira', 2001, 'F'), ('Roberto Souza', 1970, 'M'), 
('Luciana Ferreira', 1988, 'F'), ('Ricardo Alves', 1992, 'M'), ('Juliana Costa', 1997, 'F'), ('Fernando Rocha', 1985, 'M'), ('Patrícia Gomes', 1990, 'F'), 
('Bruno Mendes', 1983, 'M'), ('Beatriz Nunes', 1999, 'F'), ('Thiago Silva', 1991, 'M'), ('Camila Rocha', 1994, 'F'), ('Rodrigo Santos', 1980, 'M'), 
('Aline Vieira', 1987, 'F'), ('Gustavo Lima', 1975, 'M'), ('Larissa Melo', 1993, 'F'), ('Daniel Almeida', 1989, 'M'), ('Fernanda Costa', 1996, 'F'), 
('André Ribeiro', 1984, 'M'), ('Isabela Martins', 1998, 'F'), ('Samuel Pires', 1977, 'M'), ('Letícia Duarte', 2002, 'F'), ('Marcos Vinícius', 1981, 'M'), 
('Jorge Amado Silva', 1965, 'M'), ('Clara Luz', 1990, 'F'), ('Enzo Gabriel', 2010, 'M'), ('Valentina Rosa', 2008, 'F'), ('Igor Cavalera', 1972, 'M');
GO

-- 4. LIVROS REAIS (90 Iniciais + 10 Frios)
INSERT INTO Books (Title, Author, Publisher, ReleaseYear, PageCount) VALUES 
('Clean Architecture', 'Robert C. Martin', 'Prentice Hall', 2017, 432), ('Clean Code', 'Robert C. Martin', 'Prentice Hall', 2008, 464),
('The Pragmatic Programmer', 'Andrew Hunt', 'Addison-Wesley', 1999, 352), ('Refactoring', 'Martin Fowler', 'Addison-Wesley', 2018, 448),
('Design Patterns', 'Erich Gamma', 'Addison-Wesley', 1994, 395), ('Code Complete', 'Steve McConnell', 'Microsoft Press', 2004, 960),
('Working Effectively with Legacy Code', 'Michael Feathers', 'Prentice Hall', 2004, 456), ('Introduction to Algorithms', 'Thomas Cormen', 'MIT Press', 2009, 1312),
('Site Reliability Engineering', 'Betsy Beyer', 'O Reilly', 2016, 550), ('The Mythical Man-Month', 'Brooks Jr.', 'Addison-Wesley', 1975, 336),
('Domain-Driven Design', 'Eric Evans', 'Addison-Wesley', 2003, 560), ('Patterns of Enterprise Application Architecture', 'Martin Fowler', 'Addison-Wesley', 2002, 576),
('Continuous Delivery', 'Jez Humble', 'Addison-Wesley', 2010, 512), ('Test Driven Development', 'Kent Beck', 'Addison-Wesley', 2002, 240),
('Building Microservices', 'Sam Newman', 'O Reilly', 2015, 280), ('User Story Mapping', 'Jeff Patton', 'O Reilly', 2014, 280),
('Duna', 'Frank Herbert', 'Chilton Books', 1965, 412), ('Fundação', 'Isaac Asimov', 'Gnome Press', 1951, 255),
('Neuromancer', 'William Gibson', 'Ace Books', 1984, 271), ('1984', 'George Orwell', 'Secker & Warburg', 1949, 328),
('Brave New World', 'Aldous Huxley', 'Chatto & Windus', 1932, 311), ('Fahrenheit 451', 'Ray Bradbury', 'Ballantine Books', 1953, 158),
('The Left Hand of Darkness', 'Ursula K. Le Guin', 'Ace Books', 1969, 284), ('Snow Crash', 'Neal Stephenson', 'Bantam Books', 1992, 470),
('Hyperion', 'Dan Simmons', 'Doubleday', 1989, 482), ('The Martian', 'Andy Weir', 'Crown', 2011, 369),
('Do Androids Dream of Electric Sheep?', 'Philip K. Dick', 'DoubleDay', 1968, 210), ('Starship Troopers', 'Robert Heinlein', 'Putnam', 1959, 263),
('The Hobbit', 'J.R.R. Tolkien', 'George Allen', 1937, 310), ('American Gods', 'Neil Gaiman', 'Headline', 2001, 465),
('O Projeto Fênix', 'Gene Kim', 'IT Revolution Press', 2013, 352), ('A Startup Enxuta', 'Eric Ries', 'Crown Business', 2011, 296),
('Zero to One', 'Peter Thiel', 'Crown Business', 2014, 224), ('Good to Great', 'Jim Collins', 'HarperBusiness', 2001, 320),
('The Innovator''s Dilemma', 'Clayton Christensen', 'HBR Press', 1997, 286), ('Measure What Matters', 'John Doerr', 'Portfolio', 2018, 320),
('Extreme Ownership', 'Jocko Willink', 'St. Martin''s Press', 2015, 320), ('Start with Why', 'Simon Sinek', 'Portfolio', 2009, 256),
('High Output Management', 'Andrew Grove', 'Random House', 1983, 272), ('Principles', 'Ray Dalio', 'Simon & Schuster', 2017, 592),
('Thinking, Fast and Slow', 'Daniel Kahneman', 'Farrar, Straus', 2011, 499), ('The Hard Thing About Hard Things', 'Ben Horowitz', 'HarperBusiness', 2014, 304),
('Sapiens', 'Yuval Noah Harari', 'Harper', 2011, 443), ('Meditations', 'Marcus Aurelius', 'N/A', 180, 254),
('Man''s Search for Meaning', 'Viktor Frankl', 'Beacon Press', 1946, 165), ('The Power of Habit', 'Charles Duhigg', 'Random House', 2012, 371),
('Atomic Habits', 'James Clear', 'Avery', 2018, 320), ('Deep Work', 'Cal Newport', 'Grand Central', 2016, 304),
('Flow', 'Mihaly Csikszentmihalyi', 'Harper & Row', 1990, 303), ('The Republic', 'Plato', 'N/A', -375, 416),
('Beyond Good and Evil', 'Friedrich Nietzsche', 'N/A', 1886, 240), ('Guns, Germs, and Steel', 'Jared Diamond', 'W.W. Norton', 1997, 480),
('Steve Jobs', 'Walter Isaacson', 'Simon & Schuster', 2011, 656), ('Elon Musk', 'Ashlee Vance', 'Ecco', 2015, 400),
('The Diary of a Young Girl', 'Anne Frank', 'Contact Publishing', 1947, 283), ('Shoe Dog', 'Phil Knight', 'Scribner', 2016, 400),
('The Da Vinci Code', 'Dan Brown', 'Doubleday', 2003, 454), ('Gone Girl', 'Gillian Flynn', 'Crown', 2012, 432),
('The Girl with the Dragon Tattoo', 'Stieg Larsson', 'Norstedts', 2005, 465), ('The Silent Patient', 'Alex Michaelides', 'Celadon', 2019, 336);

-- Complemento para chegar a 90 livros "ativos"
DECLARE @c INT = 61;
WHILE @c <= 90
BEGIN
    INSERT INTO Books (Title, Author, Publisher, ReleaseYear, PageCount)
    VALUES (CONCAT('Mastering Engineering Vol ', @c), 'Tech Author', 'O Reilly', 2022, 300);
    SET @c = @c + 1;
END;

-- 10 LIVROS FRIOS (IDs 91-100)
INSERT INTO Books (Title, Author, Publisher, ReleaseYear, PageCount) VALUES 
('Arquitetura de IA com .NET', 'Marcelo Araújo', 'Nova Tech', 2025, 300), ('O Futuro dos Transformers', 'Ashish Vaswani', 'NIPS', 2024, 150),
('Manual do DynamicDTO', 'Framework Owner', 'GitHub Press', 2025, 100), ('Liderança Digital', 'Executivo X', 'Business Ed', 2024, 220),
('Cozinha Molecular', 'Chef Químico', 'Gourmet', 2024, 180), ('Astrofísica para Apressados', 'Neil deGrasse Tyson', 'Zahar', 2017, 192),
('Blockchain Industrial', 'Satoshi Tech', 'Crypto Ed', 2024, 310), ('Rust para Sistemas Críticos', 'Steve Klabnik', 'No Starch Press', 2024, 450),
('O Fim da Eternidade', 'Isaac Asimov', 'Aleph', 1955, 256), ('Deep Learning com TensorFlow.js', 'Cai Shanqing', 'Manning', 2020, 480);
GO

-- 5. ASSOCIAÇÃO LIVRO-CATEGORIA (DINÂMICA)
-- Buscamos os IDs reais da tabela Categories para evitar o conflito de Foreign Key
-- Associamos cada livro a uma categoria baseada no resto da divisão pelo número total de categorias
DECLARE @TotalCats INT = (SELECT COUNT(*) FROM Categories);
DECLARE @MinCatID INT = (SELECT MIN(CategoryID) FROM Categories);

INSERT INTO BookCategories (BookID, CategoryID)
SELECT 
    BookID, 
    @MinCatID + (BookID % @TotalCats) 
FROM Books;
GO

-- 6. RATINGS (Cerca de 250 registros para perfis 1-25)
DECLARE @p INT = (SELECT MIN(ProfileID) FROM Profiles);
DECLARE @maxP INT = @p + 24; -- Primeiros 25 perfis

WHILE @p <= @maxP
BEGIN
    INSERT INTO Ratings (ProfileID, BookID, Rating, ReadDate)
    SELECT TOP (10) 
           @p, BookID, (ABS(CHECKSUM(NEWID())) % 5), DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 365), GETDATE())
    FROM Books WHERE BookID <= 90 ORDER BY NEWID();
    SET @p = @p + 1;
END;
GO

-- DIAGNÓSTICO FINAL
SELECT 'Status' AS Relatorio, COUNT(*) AS Total FROM Categories UNION ALL
SELECT 'Livros', COUNT(*) FROM Books UNION ALL
SELECT 'Perfis Sem Leitura', COUNT(*) FROM Profiles p LEFT JOIN Ratings r ON p.ProfileID = r.ProfileID WHERE r.RatingID IS NULL;
GO