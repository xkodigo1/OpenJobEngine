# Git Flow – Guía de Trabajo 2026

**Modelo profesional de ramas y flujos para equipos de desarrollo**

Git Flow es una estrategia que organiza el ciclo de vida del repositorio en ramas especializadas. Su objetivo es **mantener un desarrollo ordenado**, permitiendo crear funcionalidades, lanzar versiones estables y corregir errores de producción sin afectar el trabajo en curso.

---

# 🧱 **Estructura Principal de Git Flow**

Git Flow define **dos ramas permanentes** y **tres ramas temporales**:

| Tipo de rama | Propósito | Quién la usa | Cuándo se usa |
| --- | --- | --- | --- |
| **`main`** | Rama estable en producción | Todo el equipo | Solo recibe releases y hotfixes |
| **`develop`** | Rama de integración de funcionalidades | Equipo de desarrollo | Recibe features terminadas |
| **`feature/*`** | Desarrollo de nuevas funcionalidades | Devs | Cada nueva tarea o módulo |
| **`release/*`** | Preparación final para despliegue | Líder técnico / DevOps | Antes de publicar una nueva versión |
| **`hotfix/*`** | Corrección urgente en producción | Cualquier dev encargado | Cuando hay errores críticos |

---

# 🧬 **0. Clonar Repositorio con Git Flow**

## ✔️ Paso a paso

```bash
git clone https://github.com/repo.git
cd repo
git branch -r
git checkout -b develop origin/develop
git checkout -b main origin/main
```

---

# 🛠️ **1. Inicializar Git Flow**

```bash
git flow init
```

Durante la configuración defines:

- Rama de producción: `main`
- Rama de integración: `develop`
- Prefijos:
  
    • `feature/`
    
    • `release/`
    
    • `hotfix/`

---

# 🌱 **2. Feature – Crear Nuevas Funcionalidades**

## ✔️ Funcionalidad

Una **feature** es una rama temporal para desarrollar una nueva función, pantalla, módulo o mejora.

## ✔️ Estructura

```
feature/nombre-de-feature
```

## ✔️ Crear feature

```bash
git flow feature start nombre-de-feature
```

## ✔️ Guardar cambios

Realiza tus cambios y crea commits con Conventional Commits:

```bash
feat: ✨ add dynamic pricing module

Implemented dynamic function to modify pricing module
```

## ⚠️ Importante

- Siempre antes de cerrar la feature se debe verificar que no se presenten errores de tipado ni errores de construcción del proyecto con los siguientes comandos:

```bash
npx tsc --noEmit
npm run build
```

## ✔️Finalizar feature

```bash
git flow feature finish nombre-de-feature
```

## ✔️Subir cambios

```bash
git push origin develop
```

---

## 🎯 **Utilidad de las Features**

- Aíslan cada funcionalidad.
- Evitan conflictos en `develop`.
- Permiten testing independiente.
- Mantienen control del historial y responsabilidades.

---

## ⚠️ **Aspectos importantes**

- Nunca desarrolles directamente en `develop` o `main`.
- Antes de finalizar una feature, **haz Squash** para dejar 1 solo commit limpio.
- Si el equipo avanza, debes **sincronizar la feature con develop (rebase)**.

---

# 🧼 **3. Squash – Limpiar Historial Antes de Finalizar Feature**

## ✔️ Funcionalidad

> Objetivo: Combinar múltiples commits en uno solo para un historial profesional.

## ✔️ Iniciar rebase

```bash
git rebase -i HEAD~numero_de_commits
```

## ✔️ Unificar commits

Cambiar `pick` por `squash` en los commits que se unirán.

```bash
pick 1f601de # feat: :art: Improving code and api format
pick a2df88a # style: :lipstick: Updated scroll in metrics content
pick 3a553ce # feat: :lipstick: Improved Metrics Design - Not Scroll

# Commits Unidos

pick 1f601de # feat: :art: Improving code and api format
squash a2df88a # style: :lipstick: Updated scroll in metrics content
squash 3a553ce # feat: :lipstick: Improved Metrics Design - Not Scroll
```

**NOTA:** Para el procedimiento exitoso del squash es **OBLIGATORIO** dejar el primer commit que aparece con estado "pick", en caso contrario se perderán los últimos avances pues en el squash el orden cronológico es de abajo hacia arriba.

## ✔️ Finalizar rebase

Cierra el archivo del editor de codigo y luego edita el commit, ejemplo de mensaje final:

```bash
#This is the initial commit

feat: ✨ complete PDF Functionality

Includes PDF module

#This is the second commit

feat: ✨ complete PDF export module

Includes totals calculation, formatting engine and final UI adjustments.
```

---

## 🎯 **Utilidad del Squash**

- Historial limpio y entendible
- Solo 1 commit por feature
- Facilita auditoría
- Facilita revertir cambios

---

## ⚠️ **Aspectos importantes**

- ¡Nunca hagas squash en ramas compartidas!
- Realízalo **antes** de `feature finish`.

---

# 🔄 **4. Amend – Organizar/Modificar ultimo commit**

Una vez el squash ha sido exitosamente realizado, es posible realizar una reformulación del commit del squash con:

## ✔️ Iniciar amend

```bash
git commit --amend
```

Es importante tener en cuenta que esto solo se aplica al ultimo commit, si se desea modificar commit anteriores es importante realizar primero un squash.

## ✔️ Editar commits

Ejemplo de mensaje final:

```bash
feat: :art: Improving code and api format

style: :lipstick: Updated page Design

feat: :lipstick: Improved Component

# Please enter the commit message for your changes. Lines starting
# with '#' will be ignored, and an empty message aborts the commit.
#
# Date:      Tue Dec 9 10:12:54 2025 -0500
#
# On branch feature/nombre_de_feature
# Changes to be committed:
#	modified:   src/test/test/test/test.tsx
#	modified:   src/views/test/test.tsx
#	modified:   src/views/test/test/utils/use-test.ts
#	modified:   src/views/test/test/edit-test.tsx
#	modified:   src/views/test/test/test/training-test.ts
#
```

**Ventaja:** Permite modificar los commits presentes o agregar un nuevo commit que servirá de descripción general

## ✔️ Finalizar amend

Cierra el archivo del editor de código.

---

## ⚠️ Importante

- No dejar el mensaje vacío o será abortado.
- No sincroniza inmediatamente en remoto, es necesario hacer un push

---

# 🔄 **5. Rebase – Sincronizar Feature con Develop**

## ✔️ Paso a paso

Asumiendo que estas trabajando sobre tu propia feature deberás seguir estos pasos.

```bash
git checkout develop
git fetch
git pull
git checkout feature/nombre-de-feature
git rebase origin/develop
```

Reordena tu trabajo como si siempre hubiera estado al final del historial. Si ocurre algún conflicto soluciónalo dándole prioridad a `develop` preferiblemente.

**Nota:** si no se esta seguro de como corregir los conflictos utiliza **`git rebase --abort`** para anular el rebase.

**Ventaja:** Historial lineal y profesional.

---

# 🚀 **6. Release – Preparar una Nueva Versión**

## ✔️ Funcionalidad

Una **release** organiza el proceso antes de publicar una nueva versión estable. Sirve para:

- Revisar bugs menores
- Ajustar configs
- Actualizar versiones
- Generar notas de lanzamiento
- Crear el **tag** final

## ✔️ Crear release

```bash
git flow release start v1.2.0
```

## ✔️ Finalizar release

```bash
git flow release finish v1.2.0
```

Durante este comando:

- Se fusiona en `main`
- Se fusiona en `develop`
- Se crea el **tag** de versión

Ejemplo de tag:

```
v1.2.0
🚀 New Features:
- Added PDF report generation.
- Improved site detection algorithm.

🐛 Fixes:
- Corrected data summarization bug.
```

## ✔️ Subir cambios

```bash
git push origin develop
git push origin main --tags
```

---

## 🎯 **Utilidad del Release**

- Congela el código para pruebas previas al despliegue.
- Permite documentar cambios.
- Asegura que la versión publicada es estable.
- Evita mezclar trabajo nuevo con la versión que se está liberando.

---

## ⚠️ **Aspectos importantes**

- No agregar nuevas funciones en una release.
- Solo se aceptan fixes menores.
- Confirmar que no hay cambios pendientes en develop antes de iniciar.

---

# 🔥 **7. Hotfix – Corrección Urgente en Producción**

## ✔️ Funcionalidad

Se utiliza cuando hay un fallo crítico en producción que debe corregirse de inmediato.

## ✔️ Crear hotfix

```bash
git flow hotfix start v1.2.1
```

## ✔️ Guardar cambios

Realiza los cambios y commits:

```bash
fix: 🐛 correct null pointer in user service
```

## ✔️ Finalizar hotfix

```bash
git flow hotfix finish v1.2.1
```

## ✔️ Subir cambios

```bash
git push origin develop
git push origin main --tags
```

---

## 🎯 **Utilidad del hotfix**

- Permite corregir rápido sin interrumpir el desarrollo en curso.
- Mantiene sincronización entre `main` y `develop`.
- Crea un tag que documenta la urgencia.

---

## ⚠️ **Aspectos importantes**

- No mezclar cambios ajenos al bug.
- Confirmar que realmente debe salir a producción de inmediato.
- Revisar impacto en dependencias y módulos afectados.

---

# 🍴 **9. Fork + Sincronización Git Flow**

Cuando trabajas con tu propio repo personal pero necesitas sincronizarte con uno de la organización:

## ✔️ Cambiar remotos:

```bash
git remote rename origin upstream
git remote add origin https://github.com/tu-user/tu-repo.git
```

## ✔️ Sincronizar:

```bash
git checkout develop
git fetch upstream
git merge upstream/develop
git push origin develop
```

---

# ✅ **Resumen Final: Utilidad de Cada Flujo**

| Flujo | Para qué sirve | Acciones permitidas | Acciones prohibidas |
| --- | --- | --- | --- |
| **Feature** | Crear nueva funcionalidad | commits, squash, amend, rebase | deploy, cambios directos en develop |
| **Release** | Preparar una versión estable | fixes menores, documentación | agregar nuevas features |
| **Hotfix** | Arreglar errores urgentes | fixes críticos | refactorización o mejoras |
| **Rebase** | Mantener historial lineal | feature actualizada | usar si la rama es compartida |