// 1. Verificamos que esté logueado
const token = localStorage.getItem('tokenSubasta');
if (!token) {
    window.location.href = 'index.html';
}

// 2. Interceptamos el formulario
document.getElementById('formCrearSubasta').addEventListener('submit', async function(evento) {
    evento.preventDefault();

    // 3. Capturamos los datos
    const nombre = document.getElementById('nombre').value;
    const descripcion = document.getElementById('descripcion').value;
    const precio = parseFloat(document.getElementById('precio').value);
    
    // Convertimos la fecha local del navegador a formato UTC (que es lo que espera la base de datos)
    const fechaLocal = document.getElementById('fecha').value;
    const fechaUTC = new Date(fechaLocal).toISOString();

    try {
        // 4. Mandamos la petición segura
        const respuesta = await fetch('http://localhost:5000/api/subastas', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}` // ¡Nuestro Token mágico!
            },
            body: JSON.stringify({
                nombreArticulo: nombre,
                descripcionArticulo: descripcion,
                precioBase: precio,
                fechaFin: fechaUTC
            })
        });

        if (respuesta.ok) {
            alert("¡Subasta publicada con éxito!");
            window.location.href = 'subastas.html'; // Lo devolvemos al panel principal
        } else {
            const error = await respuesta.text();
            alert(`Error al publicar: ${error}`);
        }
    } catch (error) {
        console.error(error);
        alert("Error de conexión con el servidor.");
    }
});

function cerrarSesion() {
    localStorage.removeItem('tokenSubasta');
    window.location.href = 'index.html';
}