#!/usr/bin/env node

/**
 * MCP Bridge for Revit
 *
 * This bridge connects Claude Desktop (stdio) to the Revit MCP server (Named Pipe)
 * It translates messages between the two transports bidirectionally.
 */

const net = require('net');
const readline = require('readline');

const PIPE_PATH = '\\\\.\\pipe\\revit-claude-mcp';
const RECONNECT_INTERVAL = 2000; // Try to reconnect every 2 seconds
const MAX_RECONNECT_ATTEMPTS = 10; // Maximum reconnection attempts

let pipeClient = null;
let reconnectAttempts = 0;
let isShuttingDown = false;
let messageBuffer = '';

// Create readline interface for stdin/stdout
const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
});

/**
 * Log to stderr (stdout is reserved for MCP protocol)
 */
function log(message) {
    console.error(`[Bridge] ${new Date().toISOString()} - ${message}`);
}

/**
 * Connect to Named Pipe
 */
function connectToPipe() {
    if (isShuttingDown) {
        return;
    }

    log(`Connecting to pipe: ${PIPE_PATH}`);

    pipeClient = net.connect(PIPE_PATH, () => {
        log('Connected to Revit MCP server');
        reconnectAttempts = 0;
    });

    pipeClient.on('data', (data) => {
        // Data from pipe -> stdout (to Claude Desktop)
        const message = data.toString();
        log(`Received from Revit: ${message.substring(0, 100)}...`);

        // Send to stdout
        process.stdout.write(message + '\n');
    });

    pipeClient.on('error', (err) => {
        log(`Pipe error: ${err.message}`);

        if (err.code === 'ENOENT' || err.code === 'ECONNREFUSED') {
            // Pipe not available - retry
            attemptReconnect();
        }
    });

    pipeClient.on('end', () => {
        log('Pipe connection ended');
        attemptReconnect();
    });

    pipeClient.on('close', () => {
        log('Pipe connection closed');
        if (!isShuttingDown) {
            attemptReconnect();
        }
    });
}

/**
 * Attempt to reconnect to pipe
 */
function attemptReconnect() {
    if (isShuttingDown) {
        return;
    }

    reconnectAttempts++;

    if (reconnectAttempts > MAX_RECONNECT_ATTEMPTS) {
        log(`Max reconnection attempts (${MAX_RECONNECT_ATTEMPTS}) reached. Exiting.`);
        log('Please ensure Revit is running and the MCP server is started.');
        process.exit(1);
    }

    log(`Reconnecting in ${RECONNECT_INTERVAL}ms (attempt ${reconnectAttempts}/${MAX_RECONNECT_ATTEMPTS})...`);

    setTimeout(() => {
        connectToPipe();
    }, RECONNECT_INTERVAL);
}

/**
 * Handle input from stdin (Claude Desktop)
 */
rl.on('line', (line) => {
    if (!line.trim()) {
        return;
    }

    log(`Received from Claude: ${line.substring(0, 100)}...`);

    // Send to pipe
    if (pipeClient && pipeClient.writable) {
        pipeClient.write(line + '\n');
    } else {
        log('ERROR: Pipe not connected, cannot send message');
    }
});

/**
 * Handle process termination
 */
function shutdown() {
    if (isShuttingDown) {
        return;
    }

    isShuttingDown = true;
    log('Shutting down bridge...');

    if (pipeClient) {
        pipeClient.end();
    }

    rl.close();

    setTimeout(() => {
        process.exit(0);
    }, 1000);
}

// Handle shutdown signals
process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);
process.on('exit', () => {
    log('Bridge exited');
});

// Handle uncaught errors
process.on('uncaughtException', (err) => {
    log(`Uncaught exception: ${err.message}`);
    log(err.stack);
    shutdown();
});

process.on('unhandledRejection', (reason, promise) => {
    log(`Unhandled rejection at: ${promise}, reason: ${reason}`);
    shutdown();
});

// Start the bridge
log('==================================');
log('Revit MCP Bridge starting...');
log(`Pipe path: ${PIPE_PATH}`);
log('==================================');

connectToPipe();

log('Bridge is running. Press Ctrl+C to exit.');
