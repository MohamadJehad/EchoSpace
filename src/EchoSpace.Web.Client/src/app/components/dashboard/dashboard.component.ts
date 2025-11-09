import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DashboardService, DashboardOverview, TimeSeriesData, ActiveSession, FailedLoginAttempt } from '../../services/dashboard.service';
import { NavbarComponent } from '../navbar/navbar.component';
import { ConfirmationModalComponent } from '../confirmation-modal/confirmation-modal.component';
import Chart from 'chart.js/auto';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, NavbarComponent, ConfirmationModalComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy, AfterViewInit {
  overview: DashboardOverview | null = null;
  loading = false;
  error: string | null = null;
  
  // Charts
  @ViewChild('userGrowthChart') userGrowthChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('postActivityChart') postActivityChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('loginActivityChart') loginActivityChartRef!: ElementRef<HTMLCanvasElement>;
  
  userGrowthChart: Chart | null = null;
  postActivityChart: Chart | null = null;
  loginActivityChart: Chart | null = null;
  
  // Tabs
  activeTab: 'overview' | 'sessions' | 'security' = 'overview';
  
  // Sessions
  activeSessions: ActiveSession[] = [];
  sessionsLoading = false;
  
  // Security
  failedAttempts: FailedLoginAttempt[] = [];
  securityLoading = false;
  
  // Modal
  showModal = false;
  pendingSessionId: string | null = null;
  pendingUserId: string | null = null;
  pendingAction: 'terminate-session' | 'terminate-user-sessions' | null = null;
  isProcessing = false;
  
  // Date range
  selectedDays = 30;

  constructor(private dashboardService: DashboardService) { }

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngAfterViewInit(): void {
    // Charts will be initialized after data loads
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = null;

    this.dashboardService.getOverview().subscribe({
      next: (data) => {
        this.overview = data;
        this.loading = false;
        this.loadCharts();
      },
      error: (err) => {
        this.error = 'Failed to load dashboard data';
        this.loading = false;
        console.error(err);
      }
    });
  }

  loadCharts(): void {
    setTimeout(() => {
      this.loadUserGrowthChart();
      this.loadPostActivityChart();
      this.loadLoginActivityChart();
    }, 100);
  }

  loadUserGrowthChart(): void {
    if (!this.userGrowthChartRef) return;

    this.dashboardService.getUserGrowth(this.selectedDays).subscribe({
      next: (data) => {
        this.destroyChart(this.userGrowthChart);
        this.userGrowthChart = this.createLineChart(
          this.userGrowthChartRef.nativeElement,
          data.data,
          'User Growth',
          'rgba(59, 130, 246, 0.5)',
          'rgba(59, 130, 246, 1)'
        );
      },
      error: (err) => console.error('Error loading user growth chart', err)
    });
  }

  loadPostActivityChart(): void {
    if (!this.postActivityChartRef) return;

    this.dashboardService.getPostActivity(this.selectedDays).subscribe({
      next: (data) => {
        this.destroyChart(this.postActivityChart);
        this.postActivityChart = this.createLineChart(
          this.postActivityChartRef.nativeElement,
          data.data,
          'Post Activity',
          'rgba(16, 185, 129, 0.5)',
          'rgba(16, 185, 129, 1)'
        );
      },
      error: (err) => console.error('Error loading post activity chart', err)
    });
  }

  loadLoginActivityChart(): void {
    if (!this.loginActivityChartRef) return;

    this.dashboardService.getLoginActivity(this.selectedDays).subscribe({
      next: (data) => {
        this.destroyChart(this.loginActivityChart);
        this.loginActivityChart = this.createLineChart(
          this.loginActivityChartRef.nativeElement,
          data.data,
          'Login Activity',
          'rgba(245, 158, 11, 0.5)',
          'rgba(245, 158, 11, 1)'
        );
      },
      error: (err) => console.error('Error loading login activity chart', err)
    });
  }

  createLineChart(canvas: HTMLCanvasElement, data: TimeSeriesData['data'], label: string, bgColor: string, borderColor: string): Chart {
    return new Chart(canvas, {
      type: 'line',
      data: {
        labels: data.map(d => new Date(d.date).toLocaleDateString()),
        datasets: [{
          label: label,
          data: data.map(d => d.value),
          borderColor: borderColor,
          backgroundColor: bgColor,
          borderWidth: 2,
          fill: true,
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'top'
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              stepSize: 1
            }
          }
        }
      }
    });
  }

  destroyChart(chart: Chart | null): void {
    if (chart) {
      chart.destroy();
    }
  }

  destroyCharts(): void {
    this.destroyChart(this.userGrowthChart);
    this.destroyChart(this.postActivityChart);
    this.destroyChart(this.loginActivityChart);
  }

  onTabChange(tab: 'overview' | 'sessions' | 'security'): void {
    this.activeTab = tab;
    if (tab === 'sessions') {
      this.loadActiveSessions();
    } else if (tab === 'security') {
      this.loadFailedAttempts();
    }
  }

  loadActiveSessions(): void {
    this.sessionsLoading = true;
    this.dashboardService.getActiveSessions().subscribe({
      next: (sessions) => {
        this.activeSessions = sessions;
        this.sessionsLoading = false;
      },
      error: (err) => {
        console.error('Error loading active sessions', err);
        this.sessionsLoading = false;
      }
    });
  }

  loadFailedAttempts(): void {
    this.securityLoading = true;
    this.dashboardService.getFailedLoginAttempts(50).subscribe({
      next: (attempts) => {
        this.failedAttempts = attempts;
        this.securityLoading = false;
      },
      error: (err) => {
        console.error('Error loading failed attempts', err);
        this.securityLoading = false;
      }
    });
  }

  terminateSession(sessionId: string): void {
    this.pendingSessionId = sessionId;
    this.pendingAction = 'terminate-session';
    this.showModal = true;
  }

  terminateUserSessions(userId: string): void {
    this.pendingUserId = userId;
    this.pendingAction = 'terminate-user-sessions';
    this.showModal = true;
  }

  onConfirm(): void {
    if (!this.pendingAction) return;

    this.isProcessing = true;

    if (this.pendingAction === 'terminate-session' && this.pendingSessionId) {
      this.dashboardService.terminateSession(this.pendingSessionId).subscribe({
        next: () => {
          this.isProcessing = false;
          this.showModal = false;
          this.pendingSessionId = null;
          this.pendingAction = null;
          this.loadActiveSessions();
        },
        error: (err) => {
          console.error('Error terminating session', err);
          this.isProcessing = false;
          this.showModal = false;
        }
      });
    } else if (this.pendingAction === 'terminate-user-sessions' && this.pendingUserId) {
      this.dashboardService.terminateUserSessions(this.pendingUserId).subscribe({
        next: () => {
          this.isProcessing = false;
          this.showModal = false;
          this.pendingUserId = null;
          this.pendingAction = null;
          this.loadActiveSessions();
        },
        error: (err) => {
          console.error('Error terminating user sessions', err);
          this.isProcessing = false;
          this.showModal = false;
        }
      });
    }
  }

  onCancelModal(): void {
    this.showModal = false;
    this.pendingSessionId = null;
    this.pendingUserId = null;
    this.pendingAction = null;
    this.isProcessing = false;
  }

  getModalConfig() {
    switch (this.pendingAction) {
      case 'terminate-session':
        return {
          title: 'Terminate Session',
          message: 'Are you sure you want to terminate this session? The user will be logged out.',
          confirmText: 'Terminate',
          buttonClass: 'bg-red-600 hover:bg-red-700'
        };
      case 'terminate-user-sessions':
        return {
          title: 'Terminate All Sessions',
          message: 'Are you sure you want to terminate all sessions for this user? They will be logged out from all devices.',
          confirmText: 'Terminate All',
          buttonClass: 'bg-red-600 hover:bg-red-700'
        };
      default:
        return {
          title: 'Confirm Action',
          message: 'Are you sure?',
          confirmText: 'Confirm',
          buttonClass: 'bg-blue-600 hover:bg-blue-700'
        };
    }
  }

  onDaysChange(): void {
    this.loadCharts();
  }

  formatDuration(duration: string): string {
    // Duration comes as "d.hh:mm:ss" or "hh:mm:ss" from backend
    if (!duration) return '0s';
    
    const parts = duration.split(':');
    if (parts.length === 3) {
      const days = parts[0].includes('.') ? parseInt(parts[0].split('.')[0] || '0', 10) : 0;
      const hours = parseInt(parts[0].split('.').pop() || '0', 10);
      const minutes = parseInt(parts[1] || '0', 10);
      const seconds = parseInt(parts[2] || '0', 10);
      
      if (days > 0) {
        return `${days}d ${hours}h`;
      } else if (hours > 0) {
        return `${hours}h ${minutes}m`;
      } else if (minutes > 0) {
        return `${minutes}m ${seconds}s`;
      } else {
        return `${seconds}s`;
      }
    }
    
    return duration;
  }
}
