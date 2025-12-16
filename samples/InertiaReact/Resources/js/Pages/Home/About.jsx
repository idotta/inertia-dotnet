import React from 'react';

export default function About({ title, description }) {
    return (
        <div className="page-about">
            <h1>{title}</h1>
            <p className="lead">{description}</p>

            <div className="card">
                <h2>What is Inertia.js?</h2>
                <p>
                    Inertia.js is a protocol for building server-driven single-page applications.
                    It allows you to build modern, reactive frontends using your favorite frontend
                    framework (React, Vue, or Svelte), while keeping all your business logic
                    server-side.
                </p>
            </div>

            <div className="card">
                <h2>Why Use Inertia.js?</h2>
                <ul>
                    <li>No need to build an API</li>
                    <li>Server-side routing</li>
                    <li>Fast SPA navigation</li>
                    <li>Keep business logic secure on the server</li>
                    <li>Use modern frontend tooling</li>
                </ul>
            </div>
        </div>
    );
}
