/* 
    Juana Paulina Águila Hernández
    Script de prueba para Grupo San Carlos
*/
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'GrupoSanCarlos_Test')
BEGIN
    CREATE DATABASE [GrupoSanCarlos_Test];
END
GO

USE [GrupoSanCarlos_Test];
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuarios]') AND type in (N'U'))
BEGIN
    CREATE TABLE Usuarios (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NombreUsuario VARCHAR(50) NOT NULL,
        Password VARCHAR(255) NOT NULL
    );
END
GO