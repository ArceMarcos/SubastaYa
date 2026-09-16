// 1. Verificamos si el usuario está logueado
const token = localStorage.getItem('tokenSubasta');
if (!token) {
    alert("Debes iniciar sesión primero.");
    window.location.href = 'index.html';
}

// 2. Función para traer las subastas
async function cargarSubastas() {
    try {
        const respuesta = await fetch('http://localhost:5000/api/subastas', {
            method: 'GET',
            // ¡AQUÍ ESTÁ LA MAGIA DEL JWT! Mandamos el token en la cabecera
            headers: {
                'Authorization': `Bearer ${token}` 
            }
        });

        if (respuesta.ok) {
            const subastas = await respuesta.json();
            renderizarSubastas(subastas);
        } else {
            console.error("Error al traer subastas", respuesta.status);
        }
    } catch (error) {
        console.error("Error de conexión:", error);
    }
}

// 3. Dibujamos el HTML
function renderizarSubastas(subastas) {
    const contenedor = document.getElementById('contenedor-subastas');
    contenedor.innerHTML = ''; // Limpiamos el "Cargando..."

    if (subastas.length === 0) {
        contenedor.innerHTML = '<p>No hay subastas activas en este momento.</p>';
        return;
    }

    subastas.forEach(sub => {
        const tarjeta = document.createElement('div');
        tarjeta.className = 'tarjeta-subasta';
        tarjeta.innerHTML = `
            <h3>${sub.nombre}</h3>
            <p>${sub.descripcion}</p>
            <p class="precio">Precio Actual: $${sub.precioActual}</p>
            <p class="fecha">Finaliza: ${new Date(sub.fechaFin).toLocaleString()}</p>
            <button onclick="pujar(${sub.id}, ${sub.precioActual})">Ofertar</button>
        `;
        contenedor.appendChild(tarjeta);
    });
}

function cerrarSesion() {
    localStorage.removeItem('tokenSubasta');
    window.location.href = 'index.html';
}

// Arrancamos la carga apenas se abre la página
cargarSubastas();

async function pujar(subastaId, precioActual) {
    // 1. Le pedimos al usuario que ingrese un monto
    const montoStr = prompt(`El precio actual es $${precioActual}.\n¿Cuánto dinero deseas ofertar?`);
    
    if (!montoStr) return; // Si el usuario presiona "Cancelar"

    const monto = parseFloat(montoStr);
    if (isNaN(monto) || monto <= precioActual) {
        alert("Por favor, ingresa un número válido y mayor al precio actual.");
        return;
    }

    // 2. Enviamos la oferta a la API enviando nuestro Token
    try {
        const respuesta = await fetch(`http://localhost:5000/api/subastas/${subastaId}/pujar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('tokenSubasta')}` // ¡Pase VIP!
            },
            body: JSON.stringify({ monto: monto })
        });

        if (respuesta.ok) {
            alert("¡Puja realizada con éxito! Eres el nuevo ganador.");
            cargarSubastas(); // Recargamos la lista para ver el precio actualizado
        } else {
            const error = await respuesta.text(); // Leemos el error del backend (ej: "Saldo insuficiente")
            alert(`No se pudo realizar la oferta:\n${error}`);
        }
    } catch (error) {
        console.error("Error de conexión:", error);
        alert("No se pudo conectar con el servidor.");
    }
}