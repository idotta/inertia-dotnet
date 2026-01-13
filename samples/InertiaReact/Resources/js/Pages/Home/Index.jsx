import React from 'react';

export default function Index({ message, features }) {
    return (
        <div className="page-home">
            <h1>🚀 {message}</h1>
            
            <div className="card">
                <h2>Features Demonstrated</h2>
                <ul className="features-list">
                    {features.map((feature, index) => (
                        <li key={index}>{feature}</li>
                    ))}
                </ul>
            </div>

            <div className="card">
                <h2>Getting Started</h2>
                <p>
                    Navigate to the <a href="/users">Users</a> page to see a full CRUD example
                    with forms, validation, and property types.
                </p>
            </div>
        </div>
    );
}
