# Proyect-Pokemon
## Proyecto Intermodular 2025/2026 IES Miguel Romero Esteo

## Guía de Ejecución en Entorno de Desarrollo

Este documento describe el procedimiento para descargar, configurar y ejecutar el proyecto **Project Pokémon** en un entorno local.

---

## 1. Requisitos Previos

### 1.1 Software necesario

| Herramienta        | Versión recomendada      |
|--------------------|--------------------------|
| Sistema Operativo  | Windows 11               |
| .NET SDK?          | 10                       |
| Visual Studio      | 2026                     |
| Node.js            | 24                       |
| Angular CLI        | 21                       |
| Git                | latest                   |
| ???                | ???                      |

### 1.2 Comprobación de versiones

```powershell
dotnet --version
node --version
ng version
git --version
```
---

## 2. Descarga del repositorio
```powershell
git clone https://github.com/Proyect-Pokemon/Project-Pokemon.git
cd project-pokemon
```
---

## 3. Ejecución del Backend (ASP.NET)
### 3.1 Abrir la solución
Abrir el fichero:
```powershell
backend/ProjectPokemon.slnx
```
con Visual Studio 2026

---

## 4. Ejecución del Frontend (Angular)
```powershell
cd frontend
npm install
ng serve
```
