const token = localStorage.getItem('tokenSubasta');
if (!token) window.location.href = 'index.html';

// Traer saldos al iniciar
async function cargarSaldos() {
    try {
        const respuesta = await fetch('http://localhost:5000/api/usuarios/billetera', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (respuesta.ok) {
            const datos = await respuesta.json();
            document.getElementById('saldoDisponible').innerText = `$${datos.saldoDisponible}`;
            document.getElementById('saldoRetenido').innerText = `$${datos.saldoRetenido}`;
        }
    } catch (error) {
        console.error("Error al cargar saldos:", error);
    }
}

// Interceptar el formulario de depósito
document.getElementById('formDepositar').addEventListener('submit', async function(evento) {
    evento.preventDefault();
    const monto = parseFloat(document.getElementById('monto').value);

    try {
        const respuesta = await fetch('http://localhost:5000/api/usuarios/depositar', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({ monto: monto })
        });

        if (respuesta.ok) {
            alert("¡Depósito realizado con éxito!");
            document.getElementById('monto').value = '';
            cargarSaldos(); // Refresca los números automáticamente
        } else {
            alert("Error al procesar el depósito.");
        }
    } catch (error) {
        alert("Error de conexión.");
    }
});

function cerrarSesion() {
    localStorage.removeItem('tokenSubasta');
    window.location.href = 'index.html';
}

// Ejecutar apenas carga la web
cargarSaldos();