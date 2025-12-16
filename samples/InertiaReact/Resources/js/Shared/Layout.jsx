import React from 'react';
import { Link, usePage } from '@inertiajs/react';

export default function Layout({ children }) {
    const { appName, flash } = usePage().props;

    return (
        <div className="layout">
            <nav className="navbar">
                <div className="container">
                    <div className="nav-brand">
                        <Link href="/">{appName}</Link>
                    </div>
                    <ul className="nav-menu">
                        <li><Link href="/">Home</Link></li>
                        <li><Link href="/users">Users</Link></li>
                        <li><Link href="/about">About</Link></li>
                    </ul>
                </div>
            </nav>

            {flash && flash.success && (
                <div className="flash flash-success">
                    {flash.success}
                </div>
            )}

            {flash && flash.error && (
                <div className="flash flash-error">
                    {flash.error}
                </div>
            )}

            <main className="main-content">
                <div className="container">
                    {children}
                </div>
            </main>

            <footer className="footer">
                <div className="container">
                    <p>&copy; 2024 Inertia React Sample. Built with Inertia.js and ASP.NET Core.</p>
                </div>
            </footer>
        </div>
    );
}
