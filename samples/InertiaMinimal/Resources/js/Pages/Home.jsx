import React from 'react';

export default function Home({ message, timestamp, appName }) {
    return (
        <div className="container">
            <div className="card">
                <h1>🚀 {appName}</h1>
                <p className="message">{message}</p>
                <p className="timestamp">
                    Server time: {new Date(timestamp).toLocaleString()}
                </p>
                <div className="info">
                    <p>
                        This is a minimal Inertia.js example with ASP.NET Core and React.
                    </p>
                    <p>
                        Edit <code>Controllers/HomeController.cs</code> to change the server-side logic,
                        or <code>Resources/js/Pages/Home.jsx</code> to modify this component.
                    </p>
                </div>
            </div>
        </div>
    );
}
