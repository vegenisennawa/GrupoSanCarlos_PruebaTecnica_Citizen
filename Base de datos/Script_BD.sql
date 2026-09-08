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

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Leads]') AND type in (N'U'))
BEGIN
    CREATE TABLE Leads (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Nombre VARCHAR(255) NOT NULL,
        Apellido_Paterno VARCHAR(255) NOT NULL,
        Apellido_Materno VARCHAR(255) NULL,
        Sexo VARCHAR(10) NOT NULL,
        Clave_Edo_Nac VARCHAR(2) NOT NULL,
        Fecha_Nac DATE NOT NULL,
        Rfc_Comparacion VARCHAR(13) NOT NULL,
        Rfc_Calculado VARCHAR(13) NOT NULL,
        Curp_Comparacion VARCHAR(18) NOT NULL,
        Curp_Calculada VARCHAR(18) NOT NULL,
        CorreoElectronico VARCHAR(150) NULL,
        Telefono VARCHAR(20) NULL,
        EstadoCivil VARCHAR(50) NULL,
        FechaRegistro DATETIME NOT NULL CONSTRAINT DF_Leads_FechaRegistro DEFAULT GETDATE(),
        FechaActualizacion DATETIME NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Leads_Activo DEFAULT 1
    );
END
GO