# Guía de ejecución en entorno de desarrollo
Esta sección describe el procedimiento necesario para descargar, configurar y ejecutar el proyecto en un entorno local.

## Requisitos previos

### Software necesario

| Herramienta                   | Requisito                          |
| ----------------------------- | ---------------------------------- |
| .NET SDK                      | Versión utilizada por el proyecto  |
| Node.js                       | Versión LTS compatible             |
| pnpm                          | Última versión estable             |
| Angular CLI                   | Versión compatible con el proyecto |
| Git                           | Última versión estable             |
| Visual Studio 2022 o superior | Recomendado para el backend        |

### Comprobación de versiones

```bash
dotnet --version
node --version
pnpm --version
ng version
git --version
```

---

## Obtención del código fuente

Clonar el repositorio oficial:

```bash
git clone https://github.com/Project-Pokemon/Project-Pokemon.git
cd Project-Pokemon
```

---

## Configuración del backend

### Abrir la solución

Abrir la solución del proyecto:

```text
backend/ProjectPokemon.slnx
```

### Restaurar dependencias

```bash
dotnet restore
```

### Ejecutar la aplicación

```bash
dotnet run
```
---

## Configuración del frontend

Acceder al directorio del frontend:

```bash
cd frontend
```

Instalar dependencias:

```bash
pnpm install
```

Iniciar el servidor de desarrollo:

```bash
pnpm start
```

o

```bash
ng serve
```
---

## Despliegue

La versión pública de la aplicación se encuentra disponible en:

```text
https://projectpokemon.runasp.net
```
